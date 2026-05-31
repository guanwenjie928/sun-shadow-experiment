"""
Sun-Shadow Experiment — 数据库模型
MySQL + SQLAlchemy
"""
from datetime import datetime
from sqlalchemy import (
    Column, Integer, String, Boolean, Float, Text,
    DateTime, ForeignKey, JSON, create_engine
)
from sqlalchemy.orm import declarative_base, relationship, sessionmaker
import uuid

Base = declarative_base()


def gen_uuid() -> str:
    return str(uuid.uuid4())


class QuizSession(Base):
    """答题场次 — 每堂课一次"""
    __tablename__ = "quiz_sessions"

    id = Column(String(36), primary_key=True, default=gen_uuid)
    teacher_name = Column(String(100), default="")
    status = Column(String(20), default="waiting")  # waiting / active / finished
    started_at = Column(DateTime, default=datetime.utcnow)
    ended_at = Column(DateTime, nullable=True)

    groups = relationship("Group", back_populates="session", cascade="all, delete-orphan")
    answers = relationship("Answer", back_populates="session", cascade="all, delete-orphan")


class Group(Base):
    """小组信息"""
    __tablename__ = "groups"

    id = Column(Integer, primary_key=True, autoincrement=True)
    session_id = Column(String(36), ForeignKey("quiz_sessions.id", ondelete="CASCADE"), nullable=False)
    group_number = Column(Integer, nullable=False)  # 1-12
    device_token = Column(String(200), default="")   # 设备标识
    joined_at = Column(DateTime, default=datetime.utcnow)

    session = relationship("QuizSession", back_populates="groups")


class Question(Base):
    """题库 — 预置题目"""
    __tablename__ = "questions"

    id = Column(Integer, primary_key=True, autoincrement=True)
    question_number = Column(Integer, nullable=False, unique=True)
    content = Column(Text, nullable=False)
    question_type = Column(String(20), nullable=False)  # single / multi / fill / essay
    options = Column(JSON, nullable=True)               # ["选项A", "选项B", ...]
    correct_answer = Column(Text, nullable=False)
    score = Column(Integer, default=10)
    hint = Column(Text, default="")                     # 提示信息


class Answer(Base):
    """学生答题记录"""
    __tablename__ = "answers"

    id = Column(Integer, primary_key=True, autoincrement=True)
    session_id = Column(String(36), ForeignKey("quiz_sessions.id", ondelete="CASCADE"), nullable=False)
    group_number = Column(Integer, nullable=False)
    question_id = Column(Integer, ForeignKey("questions.id"), nullable=False)
    answer = Column(Text, nullable=False)
    is_correct = Column(Boolean, default=False)
    score = Column(Integer, default=0)
    submitted_at = Column(DateTime, default=datetime.utcnow)

    session = relationship("QuizSession", back_populates="answers")
    question = relationship("Question")
