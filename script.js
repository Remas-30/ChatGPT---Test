const state = {
  dust: 0,
  totalDust: 0,
  perClick: 1,
  perSecond: 0,
  multiplier: 1,
  essence: 0,
  achievements: new Set(),
  lastTick: performance.now(),
};

const upgrades = [
  {
    id: 'infusion',
    name: 'Luminous Infusion',
    description: 'Doubles stardust gained per tap.',
    cost: 50,
    effect: () => {
      state.perClick *= 2;
    },
  },
  {
    id: 'resonance',
    name: 'Resonance Amplifier',
    description: 'Increase tap efficiency by 50%.',
    cost: 200,
    effect: () => {
      state.perClick *= 1.5;
    },
  },
  {
    id: 'focus',
    name: 'Quantum Focus',
    description: 'Gain +2 base stardust per tap.',
    cost: 600,
    effect: () => {
      state.perClick += 2;
    },
  },
];

const automations = [
  {
    id: 'drone',
    name: 'Spark Drones',
    description: 'Generate 2 stardust every second.',
    baseCost: 100,
    perSecond: 2,
    count: 0,
  },
  {
    id: 'forge',
    name: 'Astral Forge',
    description: 'Generate 12 stardust every second.',
    baseCost: 600,
    perSecond: 12,
    count: 0,
  },
  {
    id: 'citadel',
    name: 'Radiant Citadel',
    description: 'Generate 65 stardust every second.',
    baseCost: 2500,
    perSecond: 65,
    count: 0,
  },
];

const achievements = [
  {
    id: 'firstTap',
    icon: '✨',
    title: 'First Spark',
    description: 'Collect your first stardust.',
    condition: () => state.totalDust >= 1,
  },
  {
    id: 'hundred',
    icon: '💫',
    title: 'Stellar Apprentice',
    description: 'Accumulate 100 stardust.',
    condition: () => state.totalDust >= 100,
  },
  {
    id: 'thousand',
    icon: '🌌',
    title: 'Galactic Visionary',
    description: 'Accumulate 1,000 stardust.',
    condition: () => state.totalDust >= 1000,
  },
  {
    id: 'speed',
    icon: '⚡',
    title: 'Blazing Output',
    description: 'Reach 150 stardust per second.',
    condition: () => state.perSecond >= 150,
  },
  {
    id: 'prestige',
    icon: '🚀',
    title: 'The Ascension',
    description: 'Perform your first Nebula Ascension.',
    condition: () => state.essence >= 1,
  },
];

const upgradeList = document.getElementById('upgradeList');
const autoList = document.getElementById('autoList');
const achievementList = document.getElementById('achievementList');
const dustEl = document.getElementById('dust');
const perClickEl = document.getElementById('perClick');
const perSecondEl = document.getElementById('perSecond');
const forgeBtn = document.getElementById('forge');
const forgeBonusEl = document.getElementById('forgeBonus');
const progressBar = document.getElementById('progressBar');
const essenceEl = document.getElementById('essence');
const prestigeButton = document.getElementById('prestigeButton');
const essenceGainEl = document.getElementById('essenceGain');
const saveButton = document.getElementById('saveButton');
const resetButton = document.getElementById('resetButton');
const saveState = document.getElementById('saveState');

const format = (value) => {
  if (value >= 1e9) return (value / 1e9).toFixed(2) + 'B';
  if (value >= 1e6) return (value / 1e6).toFixed(2) + 'M';
  if (value >= 1e3) return (value / 1e3).toFixed(2) + 'K';
  return value.toFixed(0);
};

const computeAutomationCost = (auto) =>
  Math.floor(auto.baseCost * Math.pow(1.18, auto.count));

const computeAutomationPower = () => {
  const essenceBoost = 1 + state.essence * 0.1;
  return (
    automations.reduce((sum, auto) => sum + auto.perSecond * auto.count, 0) *
    essenceBoost
  );
};

function refreshShop() {
  upgrades.forEach((upgrade) => {
    if (!upgrade.element) return;
    if (upgrade.purchased) {
      upgrade.element.button.disabled = true;
      upgrade.element.button.textContent = 'Purchased';
      upgrade.element.description.textContent = `${upgrade.description} (Purchased)`;
    } else {
      upgrade.element.button.disabled = state.dust < upgrade.cost;
      upgrade.element.button.textContent = 'Upgrade';
      upgrade.element.description.textContent = `${upgrade.description} (Cost: ${format(
        upgrade.cost
      )})`;
    }
  });

  automations.forEach((auto) => {
    if (!auto.element) return;
    auto.element.title.textContent = `${auto.name} (x${auto.count})`;
    const cost = computeAutomationCost(auto);
    auto.element.description.textContent = `${auto.description} Cost: ${format(cost)}`;
    auto.element.button.disabled = state.dust < cost;
  });
}

function updateStats() {
  dustEl.textContent = format(state.dust);
  perClickEl.textContent = format(state.perClick * state.multiplier);
  perSecondEl.textContent = format(state.perSecond);
  forgeBonusEl.textContent = `+${format(state.perClick * state.multiplier)}`;
  essenceEl.textContent = format(state.essence);
  refreshShop();
}

function createUpgradeCard(upgrade) {
  const card = document.createElement('div');
  card.className = 'card';

  const info = document.createElement('div');
  const title = document.createElement('h3');
  title.textContent = upgrade.name;
  const description = document.createElement('p');
  description.textContent = `${upgrade.description} (Cost: ${format(
    upgrade.cost
  )})`;
  info.append(title, description);

  const button = document.createElement('button');
  button.textContent = 'Upgrade';
  button.addEventListener('click', () => buyUpgrade(upgrade.id));

  card.append(info, button);
  upgradeList.append(card);
  upgrade.element = { card, button, description };
}

function createAutomationCard(auto) {
  const card = document.createElement('div');
  card.className = 'card';

  const info = document.createElement('div');
  const title = document.createElement('h3');
  title.textContent = `${auto.name} (x${auto.count})`;
  const description = document.createElement('p');
  description.textContent = `${auto.description} Cost: ${format(
    computeAutomationCost(auto)
  )}`;
  info.append(title, description);

  const button = document.createElement('button');
  button.textContent = 'Deploy';
  button.addEventListener('click', () => buyAutomation(auto.id));

  card.append(info, button);
  autoList.append(card);
  auto.element = { card, button, title, description };
}

function createAchievementRow(achievement) {
  const li = document.createElement('li');
  li.className = 'achievement';

  const icon = document.createElement('div');
  icon.className = 'achievement__icon';
  icon.textContent = achievement.icon;

  const text = document.createElement('div');
  text.className = 'achievement__text';
  const title = document.createElement('div');
  title.className = 'achievement__title';
  title.textContent = achievement.title;
  const description = document.createElement('div');
  description.textContent = achievement.description;
  text.append(title, description);

  li.append(icon, text);
  achievementList.append(li);
  achievement.element = { li };
}

function buyUpgrade(id) {
  const upgrade = upgrades.find((u) => u.id === id);
  if (!upgrade || upgrade.purchased || state.dust < upgrade.cost) return;
  state.dust -= upgrade.cost;
  upgrade.effect();
  upgrade.purchased = true;
  state.multiplier = 1 + state.essence * 0.05;
  updateStats();
}

function buyAutomation(id) {
  const auto = automations.find((a) => a.id === id);
  if (!auto) return;
  const cost = computeAutomationCost(auto);
  if (state.dust < cost) return;
  state.dust -= cost;
  auto.count += 1;
  state.perSecond = computeAutomationPower();
  updateStats();
}

function gainDust(amount) {
  state.dust += amount;
  state.totalDust += amount;
  updateStats();
}

function prestigeGain() {
  const dustWorth = state.totalDust / 5000;
  return Math.floor(Math.pow(dustWorth, 0.85));
}

function tryPrestige() {
  const gain = prestigeGain();
  if (gain <= 0) return;
  state.essence += gain;
  state.dust = 0;
  state.totalDust = 0;
  state.perClick = 1;
  automations.forEach((auto) => {
    auto.count = 0;
  });
  upgrades.forEach((upgrade) => {
    upgrade.purchased = false;
  });
  state.multiplier = 1 + state.essence * 0.05;
  state.perSecond = 0;
  updatesFromAutomation();
  unlockAchievements();
  updateStats();
}

function updatesFromAutomation() {
  state.perSecond = computeAutomationPower();
}

function unlockAchievements() {
  achievements.forEach((achievement) => {
    if (!state.achievements.has(achievement.id) && achievement.condition()) {
      state.achievements.add(achievement.id);
      achievement.element.li.classList.add('unlocked');
    }
  });
}

function renderProgress(delta) {
  const max = 1000 / Math.max(state.perSecond, 1);
  const progress = ((delta % max) / max) * 100;
  progressBar.style.width = `${Math.min(progress, 100)}%`;
}

function tick(now) {
  const delta = now - state.lastTick;
  if (delta >= 100) {
    const seconds = delta / 1000;
    const generated = state.perSecond * seconds;
    if (generated > 0) {
      gainDust(generated);
    }
    state.lastTick = now;
  }
  renderProgress(now - state.lastTick);
  unlockAchievements();
  const gain = prestigeGain();
  essenceGainEl.textContent = gain;
  prestigeButton.disabled = gain <= 0;
  requestAnimationFrame(tick);
}

function tap() {
  const gain = state.perClick * state.multiplier;
  gainDust(gain);
  forgeBtn.classList.add('active');
  setTimeout(() => forgeBtn.classList.remove('active'), 150);
}

function save() {
  const payload = {
    dust: state.dust,
    totalDust: state.totalDust,
    perClick: state.perClick,
    perSecond: state.perSecond,
    multiplier: state.multiplier,
    essence: state.essence,
    autoCounts: automations.map((a) => a.count),
    purchasedUpgrades: upgrades.filter((u) => u.purchased).map((u) => u.id),
    achievements: Array.from(state.achievements),
    timestamp: Date.now(),
  };
  localStorage.setItem('stellar-foundry', JSON.stringify(payload));
  saveState.textContent = 'Saved ✓';
  setTimeout(() => (saveState.textContent = '\u00a0'), 2000);
}

function load() {
  const raw = localStorage.getItem('stellar-foundry');
  if (!raw) return;
  try {
    const data = JSON.parse(raw);
    Object.assign(state, {
      dust: data.dust ?? 0,
      totalDust: data.totalDust ?? 0,
      perClick: data.perClick ?? 1,
      perSecond: data.perSecond ?? 0,
      multiplier: data.multiplier ?? 1,
      essence: data.essence ?? 0,
    });
    automations.forEach((auto, index) => {
      auto.count = data.autoCounts?.[index] ?? 0;
    });
    upgrades.forEach((upgrade) => {
      upgrade.purchased = data.purchasedUpgrades?.includes(upgrade.id);
    });
    state.achievements = new Set(data.achievements ?? []);
    state.multiplier = 1 + state.essence * 0.05;
    updatesFromAutomation();
    achievements.forEach((achievement) => {
      if (state.achievements.has(achievement.id)) {
        achievement.element.li.classList.add('unlocked');
      }
    });
    updateStats();
  } catch (error) {
    console.error('Failed to load save', error);
  }
}

function reset() {
  if (!confirm('Reset your galaxy? This cannot be undone.')) return;
  localStorage.removeItem('stellar-foundry');
  window.location.reload();
}

function initStarfield() {
  const canvas = document.getElementById('starfield');
  const ctx = canvas.getContext('2d');

  const stars = Array.from({ length: 120 }, () => ({
    x: Math.random(),
    y: Math.random(),
    size: Math.random() * 1.5 + 0.2,
    speed: Math.random() * 0.0005 + 0.0001,
    twinkle: Math.random() * 0.6 + 0.4,
  }));

  function resize() {
    canvas.width = window.innerWidth;
    canvas.height = window.innerHeight;
  }

  function draw(time) {
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    const gradient = ctx.createRadialGradient(
      canvas.width * 0.3,
      canvas.height * 0.2,
      0,
      canvas.width * 0.3,
      canvas.height * 0.2,
      canvas.width * 0.8
    );
    gradient.addColorStop(0, 'rgba(255, 126, 226, 0.18)');
    gradient.addColorStop(1, 'rgba(5, 1, 15, 0.05)');
    ctx.fillStyle = gradient;
    ctx.fillRect(0, 0, canvas.width, canvas.height);

    stars.forEach((star) => {
      star.y += star.speed * canvas.height;
      if (star.y > 1) star.y = 0;
      const alpha = 0.5 + Math.sin(time * star.twinkle) * 0.5;
      ctx.fillStyle = `rgba(255, 255, 255, ${alpha})`;
      ctx.beginPath();
      ctx.arc(star.x * canvas.width, star.y * canvas.height, star.size, 0, Math.PI * 2);
      ctx.fill();
    });

    requestAnimationFrame(draw);
  }

  window.addEventListener('resize', resize);
  resize();
  requestAnimationFrame(draw);
}

function init() {
  upgrades.forEach(createUpgradeCard);
  automations.forEach(createAutomationCard);
  achievements.forEach(createAchievementRow);
  forgeBtn.addEventListener('click', tap);
  prestigeButton.addEventListener('click', tryPrestige);
  saveButton.addEventListener('click', save);
  resetButton.addEventListener('click', reset);
  initStarfield();
  load();
  updateStats();
  refreshShop();
  setInterval(save, 60000);
  requestAnimationFrame(tick);
}

document.addEventListener('DOMContentLoaded', init);
