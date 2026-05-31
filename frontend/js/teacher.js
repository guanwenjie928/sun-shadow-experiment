/**
 * 太阳与影子 — 教师大屏 JavaScript
 * =====================================
 * 功能: WebSocket 实时连接 → 12 宫格进度 → 排名榜
 */
const API = '/api';
let ws = null;
let currentSession = null;
let data = null;

// ─── 入口 ────────────────────────────────────────────
init();

async function init() {
  await autoJoinOrCreate();
  connectWebSocket();
  renderGrid();
}

// ─── 自动加入最新场次或创建 ───────────────────────────
async function autoJoinOrCreate() {
  try {
    const res = await fetch(`${API}/session/latest`);
    if (res.ok) {
      const session = await res.json();
      currentSession = session.session_id;
      document.getElementById('session-badge').textContent =
        `场次: ${currentSession.substring(0, 8)}...`;
    } else {
      await createNewSession();
    }
  } catch (e) {
    console.error('获取场次失败:', e);
  }
}

async function createNewSession() {
  try {
    const res = await fetch(`${API}/session/create`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ teacher_name: '老师' })
    });
    const session = await res.json();
    currentSession = session.session_id;
    document.getElementById('session-badge').textContent =
      `场次: ${session.session_id.substring(0, 8)}...`;

    // 重连 WebSocket
    if (ws) ws.close();
    connectWebSocket();

    // 重置显示
    data = null;
    renderGrid();
  } catch (e) {
    alert('创建场次失败: ' + e.message);
  }
}

// ─── WebSocket ────────────────────────────────────────
function connectWebSocket() {
  if (!currentSession) return;

  const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
  const url = `${protocol}//${window.location.host}/ws/teacher/${currentSession}`;

  ws = new WebSocket(url);

  ws.onopen = () => {
    document.getElementById('conn-dot').className = 'connection-dot online';
    document.getElementById('conn-text').textContent = '在线';
  };

  ws.onmessage = (event) => {
    try {
      data = JSON.parse(event.data);
      renderGrid();
      renderLeaderboard();
    } catch (e) {
      console.error('数据解析失败:', e);
    }
  };

  ws.onclose = () => {
    document.getElementById('conn-dot').className = 'connection-dot offline';
    document.getElementById('conn-text').textContent = '离线（5秒后重连）';
    setTimeout(connectWebSocket, 5000);
  };

  ws.onerror = (e) => {
    console.error('WebSocket 错误:', e);
  };
}

// ─── 12 宫格 ──────────────────────────────────────────
function renderGrid() {
  const grid = document.getElementById('group-grid');
  if (!grid) return;

  let html = '';
  for (let g = 1; g <= 12; g++) {
    const gd = data ? (data.groups || []).find(item => item.group_number === g) : null;
    const answered = gd ? gd.answered : 0;
    const total = data ? data.total_questions : 10;
    const pct = total > 0 ? Math.round((answered / total) * 100) : 0;
    const score = gd ? gd.total_score : 0;
    const maxScore = gd ? gd.max_score : 100;
    const hasAnswer = answered > 0;

    html += '<div class="group-card' + (hasAnswer ? ' has-answer' : '') + '">';
    html += `<div class="group-num">${g}</div>`;
    html += `<div class="group-status">第 ${g} 组</div>`;

    // 进度条
    html += '<div class="group-progress">';
    html += `<div class="bar" style="width:${pct}%"></div>`;
    html += '</div>';

    html += `<div style="font-size:.8rem;color:var(--text-secondary);">${answered}/${total} 题</div>`;
    html += `<div class="group-score">${score}/${maxScore}</div>`;

    // 展开详情
    if (gd && gd.detail && gd.detail.length > 0) {
      html += `<span class="detail-toggle" onclick="toggleDetail(this)">展开详情 ▾</span>`;
      html += '<div class="detail-panel">';
      gd.detail.forEach(d => {
        const qNum = data.questions?.find(q => q.id === d.question_id)?.number || '?';
        const icon = d.is_correct === true ? '✅' : d.is_correct === null ? '⏳' : '❌';
        html += `<div>${icon} Q${qNum}: ${d.answer} (${d.score}分)</div>`;
      });
      html += '</div>';
    }

    html += '</div>';
  }

  grid.innerHTML = html;
}

// ─── 排名榜 ────────────────────────────────────────────
function renderLeaderboard() {
  const list = document.getElementById('leader-list');
  if (!list || !data || !data.groups) return;

  const sorted = [...data.groups].sort((a, b) => b.total_score - a.total_score);

  let html = '';
  sorted.forEach((g, i) => {
    let rankClass = '';
    let rankIcon = `${i + 1}`;
    if (i === 0) { rankClass = 'gold'; rankIcon = '🥇'; }
    else if (i === 1) { rankClass = 'silver'; rankIcon = '🥈'; }
    else if (i === 2) { rankClass = 'bronze'; rankIcon = '🥉'; }

    const max = g.max_score || 100;
    const pct = max > 0 ? Math.round((g.total_score / max) * 100) : 0;

    html += '<div class="leader-row">';
    html += `<span class="rank ${rankClass}">${rankIcon}</span>`;
    html += `<span style="font-weight:600;width:60px;">第 ${g.group_number} 组</span>`;
    html += '<div class="leader-bar-wrap">';
    html += `<div class="bar" style="width:${pct}%">${g.total_score}分</div>`;
    html += '</div>';
    html += `<span style="font-size:.8rem;color:var(--text-secondary);">${g.answered}题</span>`;
    html += '</div>';
  });

  list.innerHTML = html;
}

// ─── 展开/折叠详情 ─────────────────────────────────────
function toggleDetail(el) {
  const panel = el.nextElementSibling;
  if (panel) {
    panel.classList.toggle('show');
    el.textContent = panel.classList.contains('show') ? '收起 ▲' : '展开详情 ▾';
  }
}

// ─── 一键结束 ──────────────────────────────────────────
async function finishSession() {
  if (!currentSession) return;
  if (!confirm('确定结束本场答题？结束后学生无法继续提交。')) return;

  try {
    await fetch(`${API}/session/${currentSession}/finish`, { method: 'POST' });
    document.getElementById('btn-finish').textContent = '已结束';
    document.getElementById('btn-finish').disabled = true;
    document.getElementById('btn-finish').style.opacity = '0.5';
  } catch (e) {
    alert('操作失败: ' + e.message);
  }
}
