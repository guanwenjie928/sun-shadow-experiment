"""
Sun-Shadow Experiment — 数据库连接与初始化
"""
import os
from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker
from models import Base, Question

# 数据库连接（MySQL）
MYSQL_USER = os.environ.get("MYSQL_USER", "root")
MYSQL_PASS = os.environ.get("MYSQL_PASS", "")
MYSQL_HOST = os.environ.get("MYSQL_HOST", "localhost")
MYSQL_PORT = os.environ.get("MYSQL_PORT", "3306")
MYSQL_DB = os.environ.get("MYSQL_DB", "sun_shadow_experiment")

# 先连接到无数据库的 URL 来创建数据库
BASE_URL = f"mysql+pymysql://{MYSQL_USER}:{MYSQL_PASS}@{MYSQL_HOST}:{MYSQL_PORT}"
DATABASE_URL = f"{BASE_URL}/{MYSQL_DB}?charset=utf8mb4"

engine = None
SessionLocal = None


def init_db():
    """初始化数据库：创建库、建表、插入预置题目"""
    global engine, SessionLocal

    # 创建数据库（如不存在）
    import pymysql
    try:
        conn = pymysql.connect(
            host=MYSQL_HOST, port=int(MYSQL_PORT),
            user=MYSQL_USER, password=MYSQL_PASS or "",
            charset='utf8mb4'
        )
        with conn.cursor() as cur:
            cur.execute(
                f"CREATE DATABASE IF NOT EXISTS `{MYSQL_DB}` "
                f"DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci"
            )
        conn.close()
    except Exception as e:
        print(f"[DB] Warning creating database: {e}")

    # 连接引擎
    engine = create_engine(DATABASE_URL, pool_size=10, max_overflow=20, pool_recycle=3600)
    SessionLocal = sessionmaker(bind=engine, autocommit=False, autoflush=False)

    # 建表
    Base.metadata.create_all(bind=engine)

    # 插入预置题目（幂等）
    db = SessionLocal()
    try:
        if db.query(Question).count() == 0:
            seed_questions(db)
            db.commit()
            print("[DB] Seeded 10 quiz questions.")
    finally:
        db.close()


def seed_questions(db):
    """预置 10 道题目（对齐教科版三年级下册课标）"""
    questions = [
        Question(
            question_number=1,
            content="影子产生需要哪三个条件？",
            question_type="multi",
            options=["光源", "阻挡光的物体", "能显示影子的屏", "风"],
            correct_answer="光源,阻挡光的物体,能显示影子的屏",
            score=10,
            hint="想一想：光被挡住后，墙上的暗区就是什么？"
        ),
        Question(
            question_number=2,
            content="早上 8 点，旗杆的影子指向哪个方向？",
            question_type="single",
            options=["东", "西", "南", "北"],
            correct_answer="西",
            score=10,
            hint="太阳从东边升起，影子朝向相反方向"
        ),
        Question(
            question_number=3,
            content="一天之中，什么时候影子最短？",
            question_type="single",
            options=["早上 6:00", "上午 10:00", "中午 12:00 左右", "下午 4:00"],
            correct_answer="中午 12:00 左右",
            score=10,
            hint="太阳升得最高的时候..."
        ),
        Question(
            question_number=4,
            content="太阳升得越高，地面上物体的影子会____。",
            question_type="fill",
            options=None,
            correct_answer="越短",
            score=10,
            hint="动手拖一拖太阳，从早拖到中午看看！"
        ),
        Question(
            question_number=5,
            content="在实验中把太阳拖到正午 12:00，旗杆的影子长度大约是多少米？（请观察后填写）",
            question_type="fill",
            options=None,
            correct_answer="",  # 开放答案，教师人工评判
            score=10,
            hint="看地面上的标尺，读出影子尖端的刻度"
        ),
        Question(
            question_number=6,
            content="下午 4:00 的影子方向与上午 8:00 相比，变化是？",
            question_type="single",
            options=["完全一样", "方向相反", "方向不确定", "没有变化"],
            correct_answer="方向相反",
            score=10,
            hint="早上影子朝西，下午影子朝哪儿？"
        ),
        Question(
            question_number=7,
            content="阴天的时候，为什么看不到清晰的影子？",
            question_type="essay",
            options=None,
            correct_answer="",  # 简答题，教师评判
            score=10,
            hint="阴天太阳被什么挡住了？"
        ),
        Question(
            question_number=8,
            content="冬天和夏天，同一时刻（如下午 2:00）的影子长短一样吗？请先猜再在实验中验证。",
            question_type="single",
            options=["一样长", "冬天更长", "夏天更长", "无法确定"],
            correct_answer="冬天更长",
            score=10,
            hint="切换到冬季，看看同一时间的影子有什么变化？冬天的太阳是不是更低了？"
        ),
        Question(
            question_number=9,
            content="如果太阳正好在旗杆的正上方（头顶），影子会出现在哪里？",
            question_type="single",
            options=["正前方", "正后方", "正下方（几乎没有影子）", "左边"],
            correct_answer="正下方（几乎没有影子）",
            score=10,
            hint="中午时影子最短，如果太阳在头顶正上方呢？"
        ),
        Question(
            question_number=10,
            content="古代中国人用什么工具来利用影子计时？",
            question_type="single",
            options=["日晷（guǐ）", "沙漏", "水钟", "指南针"],
            correct_answer="日晷（guǐ）",
            score=10,
            hint="影子会随着太阳移动而改变方向，古人用一个圆盘和指针来读时间"
        ),
    ]
    for q in questions:
        db.add(q)
