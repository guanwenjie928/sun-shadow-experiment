/**
 * 太阳与影子 — 学生答题端 JavaScript
 * ========================================
 * 功能: 12 小组登录 → 选择场次 → 逐题答题 → 提交 → 显示结果
 */
const API = '/api';
let currentGroup = null;
let currentSession = null;
let questions = [];
let currentQ = 0;
let answers = {};
let finished = false;

// ─── 入口 ────────────────────────────────────────────
init();

function init() {
  // 从 URL 读取组号: ?group=3
  const params = new URLSearchParams(window.location.search);
  const groupFromUrl = parseInt(params.get('group'));
  if (groupFromUrl >= 1 && groupFromUrl <= 12) {
    currentGroup = groupFromUrl;
  }
  renderLogin();
}

// ─── 步骤 1: 登录选组 ────────────────────────────────
function renderLogin() {
  let html = '<div class="card login-step">';
  html += '<h2 style="margin-bottom:16px;">👋 欢迎！请确认小组号</h2>';

  if (currentGroup) {
    html += `<p style="font-size:1.2rem;margin-bottom:12px;">当前小组：<b style="color:var(--primary);">第 ${currentGroup} 组</b></p>`;
    html += '<p style="color:var(--text-secondary);margin-bottom:16px;">点击下方按钮获取场次并开始答题</p>';
    html += '<button class="btn btn-primary" onclick="startQuiz()">开始答题</button>';
  } else {
    html += '<p style="margin-bottom:8px;">选择小组号（1-12）</p>';
    html += '<select id="group-select" onchange="currentGroup=parseInt(this.value)">';
    html += '<option value="">— 请选择 —</option>';
    for (let i = 1; i <= 12; i++) {
      html += `<option value="${i}">第 ${i} 组</option>`;
    }
    html += '</select><br>';
    html += '<button class="btn btn-primary" onclick="startQuiz()" style="margin-top:16px;">确认并开始</button>';
  }

  html += '</div>';
  document.getElementById('app').innerHTML = html;
}

// ─── 步骤 2: 获取场次和题目 ────────────────────────────
async function startQuiz() {
  if (!currentGroup) {
    currentGroup = parseInt(document.getElementById('group-select').value);
    if (!currentGroup || currentGroup < 1 || currentGroup > 12) {
      alert('请选择正确的小组号（1-12）');
      return;
    }
  }

  try {
    // 获取最新场次
    const sessionRes = await fetch(`${API}/session/latest`);
    let sessionId;

    if (sessionRes.ok) {
      const data = await sessionRes.json();
      sessionId = data.session_id;
    } else {
      // 无场次则创建（简化场景：自动创建）
      const createRes = await fetch(`${API}/session/create`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ teacher_name: '老师' })
      });
      const data = await createRes.json();
      sessionId = data.session_id;
    }

    currentSession = sessionId;

    // 获取题目
    const qRes = await fetch(`${API}/questions`);
    questions = await qRes.json();
    answers = {};
    finished = false;
    currentQ = 0;

    renderQuestion();
  } catch (e) {
    alert('连接服务器失败，请检查网络后重试。\n' + e.message);
  }
}

// ─── 步骤 3: 渲染题目 ──────────────────────────────────
function renderQuestion() {
  if (finished) { renderResult(); return; }

  const q = questions[currentQ];
  if (!q) { finishQuiz(); return; }

  let html = '';
  html += `<span class="group-badge">第 ${currentGroup} 组</span>`;

  // 进度条
  html += '<div class="progress">';
  for (let i = 0; i < questions.length; i++) {
    let cls = '';
    if (answers[questions[i].id] !== undefined) cls = 'done';
    if (i === currentQ) cls = 'current';
    html += `<span class="dot ${cls}">${i + 1}</span>`;
  }
  html += '</div>';

  // 题目卡片
  html += '<div class="card question-card">';
  html += `<h3><span class="q-num">第 ${q.question_number} 题</span> (${getTypeLabel(q.type)}) ${q.content}</h3>`;

  // 答题区域
  const prevAnswer = answers[q.id] || '';
  if (q.type === 'single') {
    html += '<div class="options">';
    (q.options || []).forEach(opt => {
      const sel = prevAnswer === opt ? ' selected' : '';
      html += `<button class="option${sel}" onclick="selectSingle('${escapeHtml(opt)}', this)">${opt}</button>`;
    });
    html += '</div>';
  } else if (q.type === 'multi') {
    const selectedList = prevAnswer ? prevAnswer.split(',').map(s => s.trim()) : [];
    html += '<div class="options" id="multi-options">';
    (q.options || []).forEach(opt => {
      const sel = selectedList.includes(opt) ? ' selected' : '';
      html += `<button class="option${sel}" onclick="toggleMulti('${escapeHtml(opt)}', this)">${opt}</button>`;
    });
    html += '</div>';
    html += `<p style="font-size:.8rem;color:var(--text-secondary);margin-top:8px;">（可多选，选好后点"下一题"）</p>`;
  } else if (q.type === 'fill') {
    html += `<input class="fill-input" id="fill-answer" value="${escapeHtml(prevAnswer)}" placeholder="请填写答案...">`;
  } else if (q.type === 'essay') {
    html += `<textarea class="essay-input" id="essay-answer" placeholder="请写下你的想法...">${prevAnswer}</textarea>`;
  }
  html += '</div>';

  // 提示
  if (q.hint) {
    html += `<div class="hint-box">💡 提示：${q.hint}</div>`;
  }

  // 导航按钮
  html += '<div class="nav-btns">';
  if (currentQ > 0) {
    html += '<button class="btn btn-outline" onclick="prevQuestion()">← 上一题</button>';
  }
  if (currentQ < questions.length - 1) {
    html += `<button class="btn btn-primary" onclick="saveAndNext()">下一题 →</button>`;
  } else {
    html += '<button class="btn btn-success" onclick="saveAndFinish()">✅ 提交全部答案</button>';
  }
  html += '</div>';

  document.getElementById('app').innerHTML = html;

  // 滚动到顶部
  window.scrollTo(0, 0);
}

// ─── 答题交互 ──────────────────────────────────────────
function selectSingle(value, el) {
  const q = questions[currentQ];
  answers[q.id] = value;

  // 高亮选中
  document.querySelectorAll('.option').forEach(o => o.classList.remove('selected'));
  el.classList.add('selected');
}

function toggleMulti(value, el) {
  const q = questions[currentQ];
  let selected = answers[q.id] ? answers[q.id].split(',').map(s => s.trim()) : [];

  const idx = selected.indexOf(value);
  if (idx >= 0) {
    selected.splice(idx, 1);
    el.classList.remove('selected');
  } else {
    selected.push(value);
    el.classList.add('selected');
  }

  answers[q.id] = selected.join(',');
}

function saveCurrentAnswer() {
  const q = questions[currentQ];
  if (q.type === 'fill') {
    const input = document.getElementById('fill-answer');
    if (input) answers[q.id] = input.value.trim();
  } else if (q.type === 'essay') {
    const input = document.getElementById('essay-answer');
    if (input) answers[q.id] = input.value.trim();
  }
}

async function submitCurrentAnswer() {
  const q = questions[currentQ];
  const answerText = answers[q.id] || '';
  if (!answerText) return; // 未作答则跳过

  try {
    await fetch(`${API}/answer`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        session_id: currentSession,
        group_number: currentGroup,
        question_id: q.id,
        answer: answerText
      })
    });
  } catch (e) {
    console.error('提交失败:', e);
  }
}

async function saveAndNext() {
  saveCurrentAnswer();
  await submitCurrentAnswer();
  if (currentQ < questions.length - 1) {
    currentQ++;
    renderQuestion();
  }
}

async function prevQuestion() {
  saveCurrentAnswer();
  await submitCurrentAnswer();
  if (currentQ > 0) {
    currentQ--;
    renderQuestion();
  }
}

async function saveAndFinish() {
  saveCurrentAnswer();
  await submitCurrentAnswer();
  finishQuiz();
}

async function finishQuiz() {
  finished = true;
  renderResult();
}

function renderResult() {
  const totalQ = questions.length;
  const answeredCount = Object.keys(answers).filter(k => answers[k]).length;

  let html = '<div class="card result-card">';
  html += '<span class="emoji">🎉</span>';
  html += '<h2>答题完成！</h2>';
  html += `<p style="font-size:1.2rem;margin:12px 0;">已提交 <b>${answeredCount}</b> / ${totalQ} 题</p>`;
  html += '<p style="color:var(--text-secondary);margin-bottom:20px;">请等待老师在屏幕上公布结果</p>';
  html += '<button class="btn btn-outline" onclick="resetQuiz()">重新答题</button>';
  html += '</div>';
  document.getElementById('app').innerHTML = html;
}

function resetQuiz() {
  answers = {};
  currentQ = 0;
  finished = false;
  renderQuestion();
}

// ─── 辅助函数 ──────────────────────────────────────────
function getTypeLabel(type) {
  const map = {
    single: '单选题',
    multi: '多选题',
    fill: '填空题',
    essay: '简答题'
  };
  return map[type] || type;
}

function escapeHtml(str) {
  return str.replace(/&/g, '&amp;').replace(/</g, '&lt;')
    .replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}
