"""
Sun-Shadow Experiment — FastAPI 后端服务
==========================================
功能:
  - REST API: 创建答题场次、提交答案、获取结果
  - WebSocket: 教师大屏实时推送小组答题情况
  - 静态文件: 前端页面直出
"""
import json
import asyncio
from datetime import datetime
from pathlib import Path
from typing import Optional, Dict, List

from fastapi import FastAPI, WebSocket, WebSocketDisconnect, Query, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from fastapi.staticfiles import StaticFiles
from fastapi.responses import FileResponse, JSONResponse
from pydantic import BaseModel

from models import QuizSession, Group, Question, Answer
from database import init_db, SessionLocal, gen_uuid

# ─── 应用初始化 ────────────────────────────────────────────
ROOT = Path(__file__).parent.parent
FRONTEND_DIR = ROOT / "frontend"
STATIC_DIR = ROOT / "static"

app = FastAPI(title="Sun-Shadow Experiment API", version="1.0.0")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


# ─── 启动事件 ─────────────────────────────────────────────
@app.on_event("startup")
def startup():
    init_db()
    print("[Server] Database initialized.")
    print("[Server] Sun-Shadow Experiment API started.")


# ─── Pydantic 请求模型 ─────────────────────────────────────
class SessionCreate(BaseModel):
    teacher_name: str = ""


class AnswerSubmit(BaseModel):
    session_id: str
    group_number: int
    question_id: int
    answer: str


# ─── WebSocket 管理器 ──────────────────────────────────────
class ConnectionManager:
    """管理教师端 WebSocket 连接"""

    def __init__(self):
        # session_id -> list of websockets (通常一个课堂一个教师)
        self.connections: Dict[str, List[WebSocket]] = {}

    async def connect(self, session_id: str, ws: WebSocket):
        await ws.accept()
        if session_id not in self.connections:
            self.connections[session_id] = []
        self.connections[session_id].append(ws)
        print(f"[WS] Teacher connected to session {session_id}")

    def disconnect(self, session_id: str, ws: WebSocket):
        if session_id in self.connections:
            self.connections[session_id].remove(ws)
            if not self.connections[session_id]:
                del self.connections[session_id]

    async def broadcast(self, session_id: str, data: dict):
        """向某场次的所有教师连接广播"""
        if session_id not in self.connections:
            return
        dead = []
        for ws in self.connections[session_id]:
            try:
                await ws.send_json(data)
            except Exception:
                dead.append(ws)
        for ws in dead:
            self.disconnect(session_id, ws)


manager = ConnectionManager()


# ─── 辅助函数 ──────────────────────────────────────────────
def get_db():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()


def build_results(session_id: str) -> dict:
    """构建某场次的完整答题结果"""
    db = SessionLocal()
    try:
        session = db.query(QuizSession).filter(QuizSession.id == session_id).first()
        if not session:
            return {}

        questions = db.query(Question).order_by(Question.question_number).all()
        answers = db.query(Answer).filter(Answer.session_id == session_id).all()

        # 按小组分组统计
        group_data = {}
        for g in range(1, 13):
            group_data[g] = {
                "group_number": g,
                "answered": 0,
                "correct": 0,
                "total_score": 0,
                "max_score": len(questions) * 10,
                "detail": []
            }

        for a in answers:
            gd = group_data.get(a.group_number)
            if gd is None:
                continue
            gd["answered"] += 1
            if a.is_correct:
                gd["correct"] += 1
            gd["total_score"] += a.score
            gd["detail"].append({
                "question_id": a.question_id,
                "answer": a.answer,
                "is_correct": a.is_correct,
                "score": a.score,
                "submitted_at": a.submitted_at.isoformat() if a.submitted_at else None
            })

        # 排序（按总分降序）
        groups_sorted = sorted(group_data.values(), key=lambda x: x["total_score"], reverse=True)

        return {
            "session_id": session_id,
            "status": session.status,
            "total_questions": len(questions),
            "groups": groups_sorted,
            "questions": [
                {"id": q.id, "number": q.question_number, "type": q.question_type,
                 "content": q.content, "options": q.options, "score": q.score}
                for q in questions
            ]
        }

    finally:
        db.close()


# ─── REST API ──────────────────────────────────────────────

@app.get("/api/health")
def health_check():
    return {"status": "ok", "service": "sun-shadow-experiment"}


@app.post("/api/session/create")
def create_session(data: SessionCreate):
    """教师创建新的答题场次"""
    db = SessionLocal()
    try:
        session = QuizSession(id=gen_uuid(), teacher_name=data.teacher_name)
        db.add(session)

        # 预创建 12 个小组
        for i in range(1, 13):
            db.add(Group(session_id=session.id, group_number=i))

        db.commit()

        return {
            "session_id": session.id,
            "teacher_name": session.teacher_name,
            "groups": list(range(1, 13)),
            "started_at": session.started_at.isoformat()
        }
    finally:
        db.close()


@app.get("/api/session/latest")
def get_latest_session():
    """获取最近创建的场次"""
    db = SessionLocal()
    try:
        session = db.query(QuizSession).order_by(QuizSession.started_at.desc()).first()
        if not session:
            raise HTTPException(status_code=404, detail="暂无场次，请先创建")
        return {
            "session_id": session.id,
            "status": session.status,
            "teacher_name": session.teacher_name
        }
    finally:
        db.close()


@app.get("/api/session/{session_id}")
def get_session(session_id: str):
    """获取场次信息"""
    db = SessionLocal()
    try:
        session = db.query(QuizSession).filter(QuizSession.id == session_id).first()
        if not session:
            raise HTTPException(status_code=404, detail="场次不存在")

        groups = db.query(Group).filter(Group.session_id == session_id).all()
        return {
            "session_id": session.id,
            "status": session.status,
            "groups": [{"group_number": g.group_number, "device_token": g.device_token}
                       for g in groups]
        }
    finally:
        db.close()


@app.get("/api/questions")
def get_questions():
    """获取所有题目"""
    db = SessionLocal()
    try:
        questions = db.query(Question).order_by(Question.question_number).all()
        return [
            {
                "id": q.id,
                "question_number": q.question_number,
                "content": q.content,
                "type": q.question_type,
                "options": q.options,
                "hint": q.hint,
                "score": q.score,
            }
            for q in questions
        ]
    finally:
        db.close()


@app.post("/api/answer")
def submit_answer(data: AnswerSubmit):
    """小组提交答案"""
    db = SessionLocal()
    try:
        # 验证场次
        session = db.query(QuizSession).filter(QuizSession.id == data.session_id).first()
        if not session:
            raise HTTPException(status_code=404, detail="场次不存在")

        if session.status == "finished":
            raise HTTPException(status_code=400, detail="场次已结束")

        # 自动激活
        if session.status == "waiting":
            session.status = "active"

        # 查找题目
        question = db.query(Question).filter(Question.id == data.question_id).first()
        if not question:
            raise HTTPException(status_code=404, detail="题目不存在")

        # 判定对错
        is_correct = False
        score = 0
        if question.question_type == "essay" or question.question_type == "fill":
            # 简答/填空：自动宽松匹配或教师评判
            is_correct = None  # 待评判
            score = question.score  # 暂时给全分，教师可改
        elif question.question_type == "multi":
            # 多选题：用集合比较（忽略顺序）
            student_set = set(s.strip() for s in data.answer.split(","))
            correct_set = set(s.strip() for s in question.correct_answer.split(","))
            is_correct = student_set == correct_set
            score = question.score if is_correct else 0
        else:
            # 单选题：直接对比
            is_correct = (data.answer.strip().lower() == question.correct_answer.strip().lower())
            score = question.score if is_correct else 0

        # 保存答案（同组同题覆盖旧答案）
        existing = db.query(Answer).filter(
            Answer.session_id == data.session_id,
            Answer.group_number == data.group_number,
            Answer.question_id == data.question_id
        ).first()

        if existing:
            existing.answer = data.answer
            existing.is_correct = is_correct
            existing.score = score
            existing.submitted_at = datetime.utcnow()
        else:
            db.add(Answer(
                session_id=data.session_id,
                group_number=data.group_number,
                question_id=data.question_id,
                answer=data.answer,
                is_correct=is_correct,
                score=score,
            ))

        db.commit()

        # ── WebSocket 广播教师端 ──
        results = build_results(data.session_id)
        asyncio.run_coroutine_threadsafe(
            manager.broadcast(data.session_id, results),
            asyncio.get_event_loop()
        )

        return {
            "success": True,
            "is_correct": is_correct,
            "score": score,
            "message": "答案已提交"
        }

    finally:
        db.close()


@app.get("/api/results/{session_id}")
def get_results(session_id: str):
    """获取场次完整结果（教师端轮询）"""
    results = build_results(session_id)
    if not results:
        raise HTTPException(status_code=404, detail="场次不存在")
    return results


@app.post("/api/session/{session_id}/finish")
def finish_session(session_id: str):
    """教师结束答题"""
    db = SessionLocal()
    try:
        session = db.query(QuizSession).filter(QuizSession.id == session_id).first()
        if not session:
            raise HTTPException(status_code=404, detail="场次不存在")

        session.status = "finished"
        session.ended_at = datetime.utcnow()
        db.commit()

        return {"success": True, "message": "场次已结束"}
    finally:
        db.close()


# ─── WebSocket ─────────────────────────────────────────────

@app.websocket("/ws/teacher/{session_id}")
async def teacher_websocket(ws: WebSocket, session_id: str):
    """教师端 WebSocket — 实时接收答题更新"""
    await manager.connect(session_id, ws)
    try:
        # 首次发送当前状态
        results = build_results(session_id)
        await ws.send_json(results)

        # 保持连接，等待客户端消息（心跳或命令）
        while True:
            data = await ws.receive_text()
            msg = json.loads(data)
            if msg.get("type") == "ping":
                await ws.send_json({"type": "pong"})
            elif msg.get("type") == "refresh":
                results = build_results(session_id)
                await ws.send_json(results)

    except WebSocketDisconnect:
        pass
    except Exception as e:
        print(f"[WS] Error: {e}")
    finally:
        manager.disconnect(session_id, ws)


# ─── 静态文件 ──────────────────────────────────────────────

@app.get("/")
def serve_index():
    return FileResponse(FRONTEND_DIR / "index.html")


@app.get("/experiment")
def serve_experiment():
    return FileResponse(FRONTEND_DIR / "experiment.html")


@app.get("/student")
def serve_student(group: int = Query(1, ge=1, le=12)):
    return FileResponse(FRONTEND_DIR / "student.html")


@app.get("/teacher")
def serve_teacher():
    return FileResponse(FRONTEND_DIR / "teacher.html")


# 静态资源
app.mount("/css", StaticFiles(directory=FRONTEND_DIR / "css"), name="css")
app.mount("/js", StaticFiles(directory=FRONTEND_DIR / "js"), name="js")

# Unity WebGL 构建产物
if STATIC_DIR.exists():
    app.mount("/Build", StaticFiles(directory=STATIC_DIR / "Build"), name="unity_build")
    app.mount("/TemplateData", StaticFiles(directory=STATIC_DIR / "TemplateData"), name="unity_template")


# ─── 入口 ──────────────────────────────────────────────────
if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=9005)
