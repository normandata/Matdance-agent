namespace Matdance.Cli.Web;

public static class WebPage
{
    public const string Html = """
<!doctype html>
<html lang="zh-CN">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>Matdance Web</title>
  <link rel="icon" type="image/png" href="/favicon.png" />
  <link rel="apple-touch-icon" href="/assets/brand/matdance-icon.png" />
  <style>
    :root {
      color-scheme: dark;
      --bg0: #050711;
      --bg1: #0b1022;
      --ink: #eef5ff;
      --soft: #aab8d0;
      --faint: #63708a;
      --line: rgba(180, 205, 255, .14);
      --line2: rgba(120, 240, 190, .28);
      --panel: rgba(12, 18, 34, .74);
      --panel-solid: #101827;
      --panel2: rgba(255, 255, 255, .055);
      --green: #67f7b1;
      --cyan: #64dbff;
      --violet: #a78bfa;
      --blue: #80a7ff;
      --amber: #ffd166;
      --rose: #ff6b8a;
      --radius-xl: 30px;
      --radius-lg: 22px;
      --radius-md: 16px;
      --shadow: 0 40px 120px rgba(0,0,0,.42);
      --phi-fr: 1.618fr;
      --unit-fr: 1fr;
      --ease-smooth: cubic-bezier(.2,.8,.2,1);
      --ease-soft: cubic-bezier(.16,1,.3,1);
      --motion-fast: .16s;
      --motion-panel: .42s;
      --font: Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", "Microsoft YaHei", sans-serif;
      --app-design-width: 1360px;
      --app-design-height: 995px;
      --app-scale: 1;
    }

    * { box-sizing: border-box; }
    html, body { height: 100%; }
    body {
      margin: 0;
      overflow: hidden;
      min-height: 100%;
      display: grid;
      place-items: center;
      color: var(--ink);
      font-family: var(--font);
      background:
        radial-gradient(circle at 12% 10%, rgba(103, 247, 177, .18), transparent 34rem),
        radial-gradient(circle at 86% 12%, rgba(100, 219, 255, .16), transparent 32rem),
        radial-gradient(circle at 70% 88%, rgba(167, 139, 250, .16), transparent 38rem),
        linear-gradient(145deg, var(--bg0), var(--bg1) 55%, #050713);
    }

    body::before, body::after {
      content: "";
      position: fixed;
      inset: -20%;
      pointer-events: none;
      z-index: 0;
    }
    body::before {
      opacity: .13;
      background-image:
        linear-gradient(rgba(255,255,255,.12) 1px, transparent 1px),
        linear-gradient(90deg, rgba(255,255,255,.10) 1px, transparent 1px);
      background-size: 48px 48px;
      transform: perspective(800px) rotateX(58deg) translateY(16%);
      transform-origin: center bottom;
      mask-image: linear-gradient(to top, #000, transparent 72%);
    }
    body::after {
      opacity: .36;
      background:
        linear-gradient(100deg, transparent 35%, rgba(255,255,255,.055), transparent 58%),
        radial-gradient(circle, rgba(255,255,255,.26) 1px, transparent 1.5px);
      background-size: 100% 100%, 70px 70px;
      mask-image: radial-gradient(circle at 50% 50%, #000, transparent 68%);
    }

    .app-scale-stage {
      position: relative;
      z-index: 1;
      width: min(100vw, calc(100vh * 1360 / 995));
      height: min(100vh, calc(100vw * 995 / 1360));
      overflow: visible;
    }

    .home-shell {
      position: absolute;
      inset: 0 auto auto 0;
      z-index: 1;
      width: var(--app-design-width);
      height: var(--app-design-height);
      padding: 18px;
      display: grid;
      grid-template-rows: minmax(0, 1fr);
      gap: 14px;
      transform: scale(var(--app-scale));
      transform-origin: top left;
    }
    .home-header {
      border: 1px solid var(--line);
      border-radius: 26px;
      padding: 12px 14px;
      display: grid;
      grid-template-columns: minmax(220px, var(--unit-fr)) minmax(0, var(--phi-fr));
      align-items: center;
      gap: 14px;
      background: linear-gradient(180deg, rgba(16, 24, 42, .78), rgba(7, 11, 22, .66));
      box-shadow: 0 24px 80px rgba(0,0,0,.26), inset 0 1px 0 rgba(255,255,255,.05);
      backdrop-filter: blur(24px) saturate(1.2);
    }
    .home-title { display: flex; align-items: center; gap: 12px; min-width: 0; }
    .home-mark {
      width: 42px;
      height: 42px;
      display: grid;
      place-items: center;
      border-radius: 15px;
      color: #06130d;
      font-weight: 950;
      background: linear-gradient(135deg, var(--green), var(--cyan));
      box-shadow: 0 0 28px rgba(103,247,177,.26);
    }
    .home-title strong { display: block; letter-spacing: -.02em; }
    .home-title small { display: block; color: var(--soft); margin-top: 2px; }
    .tab-bar { display: flex; align-items: center; justify-content: flex-end; gap: 8px; min-width: 0; flex-wrap: wrap; }
    .tab-button {
      width: auto;
      min-height: 38px;
      padding: 8px 13px;
      border-radius: 999px;
      color: var(--soft);
      background: rgba(255,255,255,.045);
    }
    .tab-button.active { color: #06130d; border-color: rgba(103,247,177,.52); background: linear-gradient(135deg, var(--green), var(--cyan)); }
    .tab-button.placeholder { cursor: default; opacity: .52; border-style: dashed; }
    .tab-button.placeholder:hover { transform: none; filter: none; border-color: var(--line); }
    .tab-panel { min-height: 0; display: none; }
    .tab-panel.active { display: block; height: 100%; }
    .home-page { overflow: hidden; }
    .home-landing {
      position: relative;
      min-height: 100%;
      overflow: hidden;
      border: 1px solid rgba(103,247,177,.18);
      border-radius: 32px;
      background:
        radial-gradient(circle at 50% 50%, rgba(103,247,177,.10), transparent 32rem),
        linear-gradient(145deg, rgba(2,4,10,.98), rgba(4,9,18,.94));
      box-shadow: var(--shadow), inset 0 1px 0 rgba(255,255,255,.06);
    }
    .star-map {
      position: absolute;
      inset: 0;
      width: 100%;
      height: 100%;
      cursor: grab;
      touch-action: none;
    }
    .star-map.dragging { cursor: grabbing; }
    .star-map.hot-target { cursor: pointer; }
    .home-vignette {
      position: absolute;
      inset: 0;
      pointer-events: none;
      background:
        radial-gradient(circle at 50% 48%, transparent 0 34%, rgba(2,4,10,.34) 68%, rgba(2,4,10,.86) 100%),
        linear-gradient(180deg, rgba(5,7,17,.72), transparent 28%, rgba(5,7,17,.64));
    }
    .home-vignette::after {
      content: "";
      position: absolute;
      inset: 0;
      opacity: .18;
      background: repeating-linear-gradient(180deg, transparent 0 7px, rgba(103,247,177,.10) 8px, transparent 10px);
      mix-blend-mode: screen;
    }
    .home-ui {
      position: absolute;
      inset: 0;
      pointer-events: none;
      padding: 34px;
      display: grid;
      grid-template-rows: auto 1fr auto;
      gap: 18px;
    }
    .home-hero {
      display: grid;
      grid-template-columns: minmax(0, var(--phi-fr)) minmax(280px, var(--unit-fr));
      align-items: flex-start;
      gap: 22px;
    }
    .glitch-title {
      position: relative;
      margin: 0;
      width: max-content;
      pointer-events: auto;
      cursor: pointer;
      user-select: none;
      color: #eafff5;
      font-size: clamp(46px, 8vw, 116px);
      line-height: .84;
      letter-spacing: -.085em;
      font-weight: 950;
      text-transform: uppercase;
      text-shadow: 0 0 20px rgba(103,247,177,.40), 0 0 55px rgba(100,219,255,.18);
      animation: titleFlicker 5.2s steps(1) infinite;
    }
    .glitch-title::before, .glitch-title::after {
      content: attr(data-text);
      position: absolute;
      inset: 0;
      pointer-events: none;
      mix-blend-mode: screen;
    }
    .glitch-title:focus-visible { outline: 2px solid rgba(103,247,177,.7); outline-offset: 8px; border-radius: 12px; }
    .glitch-title::before { color: var(--cyan); transform: translate(2px, -1px); clip-path: inset(0 0 58% 0); animation: glitchSlice 2.3s infinite linear alternate-reverse; }
    .glitch-title::after { color: var(--rose); transform: translate(-2px, 1px); clip-path: inset(42% 0 0 0); animation: glitchSlice 1.7s infinite linear alternate; }
    .home-eyebrow { display: inline-flex; align-items: center; gap: 8px; margin-bottom: 12px; color: var(--green); font-size: 12px; font-weight: 850; letter-spacing: .14em; text-transform: uppercase; }
    .home-eyebrow::before { content: ""; width: 8px; height: 8px; border-radius: 50%; background: var(--green); box-shadow: 0 0 18px var(--green); }
    .home-subtitle {
      margin-top: 18px;
      color: var(--soft);
      font-size: 15px;
      letter-spacing: .22em;
      text-transform: uppercase;
    }
    .home-subtitle::after { content: "_"; color: var(--green); animation: cursorBlink .9s steps(1) infinite; }
    .planet-hud {
      width: 100%;
      max-width: 382px;
      min-width: 0;
      justify-self: end;
      padding: 15px 16px;
      border: 1px solid rgba(103,247,177,.28);
      border-radius: 20px;
      background: rgba(4, 9, 18, .58);
      box-shadow: 0 18px 58px rgba(0,0,0,.28), inset 0 1px 0 rgba(255,255,255,.06);
      backdrop-filter: blur(18px) saturate(1.2);
      transition: border-color .18s ease, box-shadow .18s ease, transform .18s ease;
    }
    .planet-hud.active { border-color: rgba(103,247,177,.48); box-shadow: 0 22px 70px rgba(0,0,0,.34), 0 0 34px rgba(103,247,177,.10), inset 0 1px 0 rgba(255,255,255,.08); transform: translateY(-2px); }
    .planet-hud .hud-kicker { color: var(--green); font-size: 11px; font-weight: 850; letter-spacing: .16em; text-transform: uppercase; }
    .planet-hud strong { display: block; margin-top: 8px; font-size: 22px; letter-spacing: -.03em; }
    .planet-hud p { margin-top: 7px; color: var(--soft); font-size: 13px; line-height: 1.65; }
    .home-help {
      align-self: end;
      display: flex;
      align-items: end;
      justify-content: space-between;
      gap: 16px;
      color: var(--soft);
      font-size: 12px;
    }
    .control-readout {
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
      max-width: 760px;
    }
    .control-readout span, .planet-chip {
      display: inline-flex;
      align-items: center;
      gap: 7px;
      padding: 8px 10px;
      border: 1px solid rgba(255,255,255,.12);
      border-radius: 999px;
      background: rgba(4, 9, 18, .44);
      backdrop-filter: blur(14px);
    }
    .planet-dock { display: flex; flex-wrap: wrap; justify-content: flex-end; gap: 8px; pointer-events: auto; }
    .planet-chip { width: auto; min-height: 0; color: var(--soft); font-size: 12px; }
    .planet-chip.active, .planet-chip:hover:not(:disabled) { color: #06130d; border-color: rgba(103,247,177,.55); background: linear-gradient(135deg, var(--green), var(--cyan)); }
    .planet-chip:disabled { cursor: not-allowed; opacity: .46; border-style: dashed; }
    .warp-overlay {
      position: absolute;
      inset: 0;
      z-index: 5;
      pointer-events: none;
      display: grid;
      place-items: center;
      opacity: 0;
      transform: scale(.96);
      transition: opacity .18s ease;
      background: radial-gradient(circle, rgba(238,255,245,.95) 0 2%, rgba(103,247,177,.42) 8%, rgba(100,219,255,.20) 18%, transparent 52%);
      mix-blend-mode: screen;
    }
    .warp-overlay.active { opacity: 1; animation: warpFlash 2s ease both; }
    .warp-core {
      width: 14px;
      height: 14px;
      border-radius: 50%;
      background: white;
      box-shadow: 0 0 40px white, 0 0 120px var(--green), 0 0 220px var(--cyan);
    }
    .easter-egg-overlay {
      position: fixed;
      inset: 0;
      z-index: 250;
      display: grid;
      place-items: center;
      padding: 28px;
      color: #f6fbff;
      background:
        radial-gradient(circle at 50% 42%, rgba(103,247,177,.16), transparent 26rem),
        linear-gradient(180deg, rgba(4,8,16,.92), rgba(2,4,10,.97));
      backdrop-filter: blur(14px) saturate(1.15);
      opacity: 0;
      pointer-events: none;
      transition: opacity .22s ease;
    }
    .easter-egg-overlay.active {
      opacity: 1;
      pointer-events: auto;
    }
    .easter-egg-card {
      width: min(760px, 92vw);
      border: 1px solid rgba(103,247,177,.28);
      border-radius: 26px;
      padding: clamp(28px, 5vw, 54px);
      background: linear-gradient(145deg, rgba(8,14,26,.88), rgba(3,7,15,.80));
      box-shadow: 0 28px 90px rgba(0,0,0,.42), inset 0 1px 0 rgba(255,255,255,.08);
      transform: translateY(10px) scale(.98);
      transition: transform .22s ease;
    }
    .easter-egg-overlay.active .easter-egg-card { transform: translateY(0) scale(1); }
    .easter-egg-quote {
      margin: 0;
      font-family: "Microsoft YaHei", "PingFang SC", "Noto Serif SC", serif;
      font-size: clamp(20px, 3.2vw, 36px);
      line-height: 1.85;
      letter-spacing: .02em;
      text-align: center;
      text-shadow: 0 0 24px rgba(103,247,177,.16);
    }
    .page-enter { animation: pageBloom .58s cubic-bezier(.18,.9,.24,1) both; transform-origin: center center; }
    @keyframes titleFlicker { 0%, 91%, 100% { opacity: 1; } 92% { opacity: .72; } 93% { opacity: 1; } 94% { opacity: .82; } }
    @keyframes glitchSlice { 0% { transform: translate(2px,-1px); } 20% { transform: translate(-3px,1px); } 40% { transform: translate(1px,2px); } 60% { transform: translate(4px,-2px); } 100% { transform: translate(-1px,1px); } }
    @keyframes cursorBlink { 50% { opacity: 0; } }
    @keyframes warpFlash { 0% { opacity: 0; transform: scale(.8); } 28% { opacity: .45; } 72% { opacity: .95; transform: scale(1.55); } 100% { opacity: 0; transform: scale(2.5); } }
    @keyframes pageBloom { from { opacity: 0; clip-path: circle(0% at 50% 50%); transform: scale(.96); filter: blur(8px); } to { opacity: 1; clip-path: circle(140% at 50% 50%); transform: none; filter: none; } }
    @keyframes goldenPanelIn { from { opacity: 0; transform: translateY(14px) scale(.985); } to { opacity: 1; transform: none; } }
    @keyframes goldenCardIn { from { opacity: 0; transform: translateY(10px); } to { opacity: 1; transform: none; } }
    @keyframes goldenSheen { 0%, 42% { transform: translateX(-120%); opacity: 0; } 52% { opacity: .42; } 72%, 100% { transform: translateX(120%); opacity: 0; } }

    @media (prefers-reduced-motion: reduce) {
      .glitch-title, .glitch-title::before, .glitch-title::after, .home-subtitle::after, .warp-overlay.active, .page-enter, .easter-egg-overlay, .easter-egg-card,
      #homePage.active .home-landing, #agentTab.active .agent-side, #agentTab.active .agent-main, #scheduleTab.active .agent-side,
      #scheduleTab.active .agent-main, #skillsTab.active .agent-side, #skillsTab.active .agent-main, #settingsTab.active .agent-side,
      #settingsTab.active .agent-main, #labTab.active .agent-side, #labTab.active .agent-main, #agentTab.active .agent-hero, #agentTab.active .control-card, #agentTab.active .agent-status-card,
      #scheduleTab.active .agent-hero, #scheduleTab.active .control-card, #skillsTab.active .agent-hero, #skillsTab.active .control-card,
      #settingsTab.active .agent-hero, #settingsTab.active .control-card, #labTab.active .agent-hero, #labTab.active .control-card, #labTab.active .note-card, .schedule-item, .schedule-history-entry, .skill-item { animation: none !important; }
      #agentTab .agent-main::before, #scheduleTab .agent-main::before, #skillsTab .agent-main::before, #settingsTab .agent-main::before, #labTab .agent-main::before { display: none !important; }
    }

    .app {
      height: 100%;
      min-height: 0;
      display: grid;
      grid-template-columns: 340px minmax(0, 1fr);
      gap: 20px;
    }

    .sidebar, .stage, .agent-side, .agent-main {
      border: 1px solid var(--line);
      background: linear-gradient(180deg, rgba(16, 24, 42, .78), rgba(7, 11, 22, .66));
      box-shadow: var(--shadow), inset 0 1px 0 rgba(255,255,255,.05);
      backdrop-filter: blur(24px) saturate(1.2);
    }

    .sidebar {
      border-radius: var(--radius-xl);
      padding: 18px;
      display: flex;
      flex-direction: column;
      gap: 16px;
      min-width: 0;
    }

    .brand-card {
      position: relative;
      overflow: hidden;
      border: 1px solid var(--line);
      border-radius: 24px;
      padding: 18px;
      background:
        linear-gradient(135deg, rgba(103,247,177,.14), rgba(100,219,255,.08) 42%, rgba(167,139,250,.10)),
        rgba(255,255,255,.04);
    }
    .brand-card::after {
      content: "";
      position: absolute;
      width: 180px;
      height: 180px;
      right: -70px;
      top: -70px;
      border-radius: 50%;
      background: radial-gradient(circle, rgba(103,247,177,.30), transparent 67%);
    }
    .brand-top { position: relative; z-index: 1; display: flex; align-items: center; gap: 12px; }
    .logo {
      width: 48px;
      height: 48px;
      display: grid;
      place-items: center;
      overflow: hidden;
      border: 1px solid rgba(255,255,255,.16);
      border-radius: 14px;
      background: radial-gradient(circle at 50% 42%, rgba(100,219,255,.16), rgba(4,8,18,.50));
      box-shadow: 0 0 34px rgba(100,219,255,.22);
    }
    .logo img { width: 100%; height: 100%; object-fit: contain; padding: 3px; filter: drop-shadow(0 0 10px rgba(100,219,255,.34)); }
    h1, h2, p { margin: 0; }
    .brand-card h1 { font-size: 20px; letter-spacing: 0; }
    .brand-card p { margin-top: 4px; color: var(--soft); font-size: 12px; }
    .brand-sub {
      position: relative;
      z-index: 1;
      margin-top: 18px;
      display: flex;
      gap: 8px;
      flex-wrap: wrap;
    }
    .chip {
      display: inline-flex;
      align-items: center;
      gap: 7px;
      padding: 7px 10px;
      border: 1px solid rgba(255,255,255,.10);
      border-radius: 999px;
      color: var(--soft);
      background: rgba(4, 8, 18, .35);
      font-size: 12px;
    }
    .chip::before { content: ""; width: 6px; height: 6px; border-radius: 50%; background: var(--green); box-shadow: 0 0 14px var(--green); }

    .control-card, .metric-card, .task-card, .note-card {
      border: 1px solid var(--line);
      border-radius: 22px;
      padding: 15px;
      background: rgba(255,255,255,.045);
    }
    .field { margin-bottom: 13px; }
    .field:last-child { margin-bottom: 0; }
    label {
      display: flex;
      justify-content: space-between;
      margin-bottom: 7px;
      color: var(--soft);
      font-size: 12px;
      letter-spacing: .02em;
      text-transform: uppercase;
    }

    select, textarea, input, button {
      width: 100%;
      border: 1px solid var(--line);
      color: var(--ink);
      background: rgba(3, 7, 16, .62);
      outline: none;
      font: inherit;
    }
    select, input {
      min-height: 45px;
      border-radius: 15px;
      padding: 0 12px;
    }
    button {
      min-height: 44px;
      border-radius: 15px;
      cursor: pointer;
      font-weight: 760;
      transition: transform .16s ease, border-color .16s ease, filter .16s ease;
    }
    button:hover:not(:disabled) { transform: translateY(-1px); border-color: var(--line2); filter: brightness(1.08); }
    button:disabled { cursor: not-allowed; opacity: .55; }
    .button-row { display: grid; grid-template-columns: 1fr 1fr; gap: 10px; }
    .primary { border-color: rgba(103,247,177,.40); background: linear-gradient(135deg, rgba(103,247,177,.28), rgba(100,219,255,.18)); }
    .ghost { background: rgba(255,255,255,.05); }

    .metric-grid { display: grid; gap: 12px; }
    .metric-line { display: flex; align-items: baseline; justify-content: space-between; gap: 12px; }
    .metric-line span { color: var(--soft); font-size: 12px; }
    .metric-line strong { font-size: 13px; text-align: right; }
    .ctx-shell { height: 11px; border-radius: 999px; background: rgba(255,255,255,.08); overflow: hidden; box-shadow: inset 0 1px 2px rgba(0,0,0,.28); }
    .ctx-fill { width: 0%; height: 100%; border-radius: inherit; background: linear-gradient(90deg, var(--green), var(--cyan), var(--violet)); transition: width .28s ease; }
    .task-card { display: grid; gap: 10px; }
    .task-head { display: flex; align-items: center; justify-content: space-between; gap: 10px; }
    .task-title { min-width: 0; }
    .task-title-row { display: grid; grid-template-columns: 18px minmax(0,1fr); gap: 8px; align-items: center; }
    .task-title strong { display: block; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .task-title span { display: block; margin-top: 3px; color: var(--soft); font-size: 12px; }
    .task-toggle { width: auto; min-height: 32px; padding: 6px 10px; border-radius: 11px; color: var(--soft); }
    .task-steps { display: none; gap: 8px; margin: 0; padding: 0; list-style: none; }
    .task-card.expanded .task-steps { display: grid; }
    .task-step { display: grid; grid-template-columns: 18px 22px minmax(0,1fr); gap: 8px; align-items: center; color: var(--soft); font-size: 12px; line-height: 1.45; }
    .task-step b { color: var(--green); font-size: 11px; }
    .task-step span:last-child { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .task-step.done { color: rgba(238,245,255,.82); }
    .task-step-icon { display: inline-grid; place-items: center; width: 18px; height: 18px; border: 1px solid rgba(255,255,255,.14); border-radius: 50%; color: var(--faint); font-size: 11px; }
    .task-step.done .task-step-icon, .task-step-icon.done { color: #06130d; border-color: transparent; background: var(--green); box-shadow: 0 0 16px rgba(103,247,177,.32); }
    .task-step.running .task-step-icon, .task-step-icon.running { color: var(--green); border-color: rgba(103,247,177,.35); border-top-color: transparent; animation: spin .8s linear infinite; }
    .task-empty { color: var(--faint); font-size: 12px; line-height: 1.5; }
    .note-card { color: var(--soft); font-size: 12px; line-height: 1.68; margin-top: auto; }
    .note-card strong { color: var(--ink); }

    .stage {
      position: relative;
      border-radius: var(--radius-xl);
      min-width: 0;
      display: grid;
      grid-template-rows: auto minmax(0, 1fr) auto;
      overflow: hidden;
    }

    .topbar {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 18px;
      padding: 18px 22px;
      border-bottom: 1px solid var(--line);
      background: linear-gradient(180deg, rgba(255,255,255,.055), rgba(255,255,255,.025));
    }
    .window-dots { display: flex; gap: 8px; margin-bottom: 10px; }
    .window-dots button {
      width: 12px;
      height: 12px;
      min-height: 0;
      padding: 0;
      border: 0;
      border-radius: 50%;
      cursor: pointer;
      font-size: 0;
      box-shadow: inset 0 0 0 1px rgba(0,0,0,.16);
    }
    .window-dots button:nth-child(1) { background: var(--rose); }
    .window-dots button:nth-child(2) { background: var(--amber); }
    .window-dots button:nth-child(3) { background: var(--green); }
    .title-block h2 { font-size: 20px; letter-spacing: -.02em; }
    .title-block small { display: block; margin-top: 4px; color: var(--soft); }
    .phase-pill {
      display: inline-flex;
      align-items: center;
      gap: 9px;
      padding: 10px 13px;
      border: 1px solid rgba(103,247,177,.28);
      border-radius: 999px;
      color: var(--soft);
      background: rgba(103,247,177,.07);
      white-space: nowrap;
      font-size: 13px;
    }
    .phase-dot { width: 8px; height: 8px; border-radius: 50%; background: var(--green); box-shadow: 0 0 18px var(--green); }

    /* Browser Overlay */
    .browser-overlay { display: none; position: fixed; inset: 0; z-index: 1000; background: rgba(5,7,17,.92); backdrop-filter: blur(12px); align-items: center; justify-content: center; }
    .browser-overlay.active { display: flex; }
    .browser-window { width: min(90vw, 1400px); height: min(85vh, 900px); display: grid; grid-template-rows: auto 1fr; border: 1px solid var(--line); border-radius: var(--radius-lg); background: var(--panel-solid); box-shadow: var(--shadow); overflow: hidden; }
    .browser-titlebar { display: flex; align-items: center; gap: 12px; padding: 12px 16px; border-bottom: 1px solid var(--line); background: linear-gradient(180deg, rgba(255,255,255,.055), rgba(255,255,255,.025)); }
    .browser-title { flex: 1; text-align: center; font-size: 13px; color: var(--soft); }
    .browser-content { position: relative; overflow: hidden; background: #000; }
    .browser-frame { width: 100%; height: 100%; object-fit: contain; display: none; }
    .browser-frame.active { display: block; }
    .browser-placeholder { position: absolute; inset: 0; display: flex; flex-direction: column; align-items: center; justify-content: center; color: var(--soft); font-size: 14px; }
    .browser-placeholder.hidden { display: none; }

    .chat {
      position: relative;
      padding: 24px;
      overflow: auto;
      display: flex;
      flex-direction: column;
      gap: 18px;
      scroll-behavior: auto;
    }
    .chat-jump-bottom {
      position: absolute;
      right: 26px;
      bottom: 112px;
      z-index: 16;
      width: auto;
      min-height: 36px;
      padding: 8px 12px;
      border-radius: 999px;
      color: #06130d;
      background: linear-gradient(135deg, var(--green), var(--cyan));
      border-color: rgba(255,255,255,.22);
      box-shadow: 0 14px 36px rgba(0,0,0,.28), 0 0 0 1px rgba(255,255,255,.12) inset;
      font-size: 12px;
      font-weight: 850;
      transform: translateY(0);
      transition: opacity var(--motion-fast) var(--ease-smooth), transform var(--motion-fast) var(--ease-smooth);
    }
    .chat-jump-bottom[hidden] { display: block; opacity: 0; pointer-events: none; transform: translateY(8px); }
    .chat::-webkit-scrollbar, .tool-result::-webkit-scrollbar { width: 10px; }
    .chat::-webkit-scrollbar-thumb, .tool-result::-webkit-scrollbar-thumb { background: rgba(255,255,255,.14); border-radius: 999px; border: 3px solid transparent; background-clip: padding-box; }

    .empty {
      margin: auto;
      max-width: 560px;
      text-align: center;
      color: var(--soft);
      line-height: 1.75;
      padding: 34px;
      border: 1px dashed rgba(255,255,255,.14);
      border-radius: 28px;
      background: rgba(255,255,255,.035);
    }
    .empty::before {
      content: "✦";
      display: block;
      color: var(--green);
      font-size: 34px;
      margin-bottom: 8px;
      text-shadow: 0 0 24px rgba(103,247,177,.45);
    }

    .message {
      width: min(860px, 92%);
      display: grid;
      grid-template-columns: 38px minmax(0, 1fr);
      gap: 11px;
      align-items: start;
      animation: rise .18s ease both;
    }
    @keyframes rise { from { opacity: 0; transform: translateY(8px) scale(.995); } to { opacity: 1; transform: none; } }
    .message.user { align-self: flex-end; grid-template-columns: minmax(0, 1fr) 38px; }
    .avatar img { width: 100%; height: 100%; object-fit: cover; border-radius: inherit; display: block; }
    .lang-toggle { position: fixed; top: 20px; right: 20px; z-index: 40; width: auto; min-height: 34px; padding: 0 12px; border-radius: 999px; color: var(--green); border-color: rgba(103,247,177,.28); background: rgba(4,9,18,.68); backdrop-filter: blur(14px); box-shadow: 0 12px 34px rgba(0,0,0,.24); }
    .danger { color: #ffd6dc; border-color: rgba(255,107,129,.28); background: rgba(255,107,129,.10); }
    .danger:hover { border-color: rgba(255,107,129,.48); background: rgba(255,107,129,.16); }
    .agent-manage-actions { margin-top: 10px; }
    .agent-avatar-card { border: 1px solid var(--line); border-radius: 22px; padding: 14px; display: grid; gap: 12px; background: rgba(255,255,255,.04); }
    .agent-avatar-top { display: flex; align-items: center; gap: 12px; }
    .agent-avatar-preview { width: 58px; height: 58px; border-radius: 20px; display: grid; place-items: center; overflow: hidden; flex: 0 0 58px; font-size: 22px; font-weight: 950; color: #06130d; background: linear-gradient(135deg,var(--green),var(--cyan)); box-shadow: 0 16px 36px rgba(0,0,0,.26); }
    .agent-avatar-preview img { width: 100%; height: 100%; object-fit: cover; display: block; }
    .agent-avatar-copy { min-width: 0; display: grid; gap: 4px; }
    .agent-avatar-copy strong { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .agent-avatar-copy span { display: none; color: var(--soft); font-size: 12px; line-height: 1.45; }
    .agent-avatar-card input[type=file] { display: none; }
    .agent-avatar-card #agentIconUpload { width: 100%; text-align: center; }
    .avatar {
      width: 38px;
      height: 38px;
      border-radius: 14px;
      display: grid;
      place-items: center;
      font-weight: 900;
      color: #07111d;
      background: linear-gradient(135deg, var(--green), var(--cyan));
      box-shadow: 0 12px 26px rgba(0,0,0,.22);
    }
    .message.user .avatar { grid-column: 2; background: linear-gradient(135deg, var(--blue), var(--violet)); color: white; }
    .message.user .stack { grid-column: 1; grid-row: 1; }
    .stack { display: grid; gap: 7px; }
    .meta {
      display: flex;
      align-items: center;
      gap: 8px;
      flex-wrap: wrap;
      min-width: 0;
      color: var(--soft);
      font-size: 12px;
      padding: 0 4px;
    }
    .message.user .meta { justify-content: flex-end; }
    .card {
      border: 1px solid var(--line);
      border-radius: 22px;
      padding: 15px 17px;
      background: rgba(8, 13, 26, .72);
      white-space: pre-wrap;
      line-height: 1.68;
      box-shadow: 0 16px 42px rgba(0,0,0,.22), inset 0 1px 0 rgba(255,255,255,.035);
    }
    .card.markdown { white-space: normal; }
    .markdown p { margin: 0 0 .72em; }
    .markdown p:last-child { margin-bottom: 0; }
    .markdown h1, .markdown h2, .markdown h3 { margin: .25em 0 .45em; line-height: 1.25; }
    .markdown h1 { font-size: 1.24rem; }
    .markdown h2 { font-size: 1.12rem; }
    .markdown h3 { font-size: 1rem; }
    .markdown ul { margin: .25em 0 .75em 1.2em; padding: 0; }
    .markdown li { margin: .24em 0; }
    .markdown a { color: var(--cyan); text-decoration: none; border-bottom: 1px solid rgba(100,219,255,.36); }
    .markdown blockquote { margin: .45em 0; padding: .28em .75em; color: var(--soft); border-left: 3px solid rgba(103,247,177,.44); background: rgba(103,247,177,.055); border-radius: 10px; }
    .markdown code { padding: .12em .38em; border-radius: 7px; color: #d8fff0; background: rgba(103,247,177,.10); }
    .markdown pre { margin: .65em 0; padding: 12px; overflow: auto; border: 1px solid rgba(255,255,255,.10); border-radius: 14px; background: rgba(0,0,0,.28); }
    .markdown pre code { padding: 0; background: transparent; color: #e8efff; }
    .preview-box-container { margin: 14px 0 16px; display: grid; gap: 12px; }
    .preview-box {
      position: relative;
      overflow: hidden;
      border: 1px solid rgba(180,205,255,.18);
      border-radius: 18px;
      background:
        linear-gradient(180deg, rgba(255,255,255,.055), rgba(255,255,255,.018)),
        rgba(9, 14, 28, .94);
      box-shadow: 0 18px 54px rgba(0,0,0,.34), inset 0 1px 0 rgba(255,255,255,.06);
    }
    .preview-box::before {
      content: "";
      position: absolute;
      inset: 0 0 auto;
      height: 2px;
      background: linear-gradient(90deg, var(--green), var(--cyan), var(--violet));
      opacity: .78;
    }
    .preview-header {
      min-height: 48px;
      padding: 10px 12px;
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 12px;
      border-bottom: 1px solid rgba(180,205,255,.12);
      background: linear-gradient(180deg, rgba(255,255,255,.055), rgba(255,255,255,.018));
    }
    .preview-file-main { min-width: 0; display: flex; align-items: center; gap: 10px; }
    .preview-file-icon {
      width: 30px;
      height: 30px;
      display: grid;
      place-items: center;
      flex: 0 0 30px;
      border: 1px solid rgba(103,247,177,.28);
      border-radius: 10px;
      color: #dfffee;
      background: linear-gradient(135deg, rgba(103,247,177,.18), rgba(100,219,255,.10));
      font-size: 10px;
      font-weight: 850;
    }
    .preview-file-copy { min-width: 0; display: grid; gap: 2px; }
    .preview-file-name { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; color: var(--ink); font-size: 13px; font-weight: 760; }
    .preview-file-meta { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; color: var(--faint); font-size: 11px; }
    .preview-actions { display: flex; align-items: center; gap: 7px; flex: 0 0 auto; }
    .preview-open-button {
      width: 32px;
      min-height: 32px;
      padding: 0;
      display: grid;
      place-items: center;
      position: relative;
      border-radius: 10px;
      color: var(--soft);
      background: rgba(255,255,255,.045);
    }
    .preview-open-button::before {
      content: "";
      width: 10px;
      height: 10px;
      border-top: 2px solid currentColor;
      border-right: 2px solid currentColor;
      transform: translate(-1px, 1px);
    }
    .preview-open-button::after {
      content: "";
      position: absolute;
      width: 13px;
      height: 2px;
      border-radius: 2px;
      background: currentColor;
      transform: rotate(-45deg) translate(-1px, 1px);
    }
    .preview-open-button:hover:not(:disabled) { color: #06130d; border-color: rgba(103,247,177,.46); background: linear-gradient(135deg, var(--green), var(--cyan)); }
    .preview-body { min-height: 88px; max-height: 460px; overflow: auto; position: relative; background: rgba(3,7,16,.34); }
    .preview-surface { position: relative; width: 100%; min-height: 160px; display: grid; place-items: center; background: rgba(0,0,0,.18); }
    .preview-surface.light { min-height: 250px; background: #f6f8fb; }
    .preview-loader {
      position: absolute;
      inset: 0;
      z-index: 1;
      display: grid;
      place-items: center;
      color: var(--faint);
      font-size: 12px;
      background: linear-gradient(180deg, rgba(6,10,20,.70), rgba(6,10,20,.44));
    }
    .preview-loader.light { color: #6b7280; background: linear-gradient(180deg, #f8fafc, #eef2f7); }
    .preview-loader.error { z-index: 3; color: #b91c1c; }
    .preview-image { max-width: 100%; display: block; margin: 0 auto; position: relative; z-index: 2; }
    .preview-audio { width: min(100%, 560px); margin: 28px auto; display: block; position: relative; z-index: 2; }
    .preview-frame { width: 100%; height: min(52vh, 380px); min-height: 280px; border: 0; background: #fff; display: block; position: relative; z-index: 2; }
    .preview-text-wrap, .preview-markdown-wrap { padding: 14px; }
    .preview-code {
      margin: 0;
      max-height: 390px;
      overflow: auto;
      border: 1px solid rgba(255,255,255,.08);
      border-radius: 12px;
      padding: 14px;
      background: rgba(0,0,0,.28);
      color: var(--soft);
      font-size: 12.5px;
      line-height: 1.62;
    }
    .preview-placeholder { padding: 24px; display: grid; justify-items: center; gap: 8px; color: var(--faint); text-align: center; font-size: 13px; }
    .preview-link { color: var(--cyan); font-size: 12px; text-decoration: none; border-bottom: 1px solid rgba(100,219,255,.34); }
    .stream-line { display: grid; grid-template-columns: 1.1em minmax(0,1fr); gap: 8px; align-items: start; }
    .stream-spacer { width: 1.1em; min-height: 1px; }
    .assistant .card { border-color: rgba(103,247,177,.20); }
    .thinking-message .avatar { opacity: .72; }
    .thinking-card {
      border: 1px solid rgba(100,219,255,.24);
      border-radius: 18px;
      padding: 13px 14px;
      color: #c8f3ff;
      background: linear-gradient(135deg, rgba(12, 45, 62, .30), rgba(25, 34, 68, .22));
      box-shadow: inset 0 0 0 1px rgba(255,255,255,.025);
    }
    .thinking-raw {
      margin: 0;
      max-height: 260px;
      overflow: auto;
      white-space: pre-wrap;
      word-break: break-word;
      font-family: "JetBrains Mono", "SFMono-Regular", Consolas, monospace;
      font-size: 12px;
      line-height: 1.62;
      color: #bdeeff;
    }
    .sound-cue-event {
      margin: 9px 0;
      padding: 9px 10px;
      border: 1px solid rgba(100,219,255,.24);
      border-radius: 14px;
      background: linear-gradient(135deg, rgba(100,219,255,.10), rgba(103,247,177,.065));
      display: grid;
      grid-template-columns: 34px minmax(0, 1fr) auto;
      grid-template-areas:
        "icon main status"
        "body body body";
      align-items: center;
      gap: 10px;
      overflow: hidden;
      box-shadow: inset 0 0 0 1px rgba(255,255,255,.025), 0 10px 28px rgba(0,0,0,.12);
    }
    .sound-cue-event.thinking {
      border-color: rgba(100,219,255,.30);
      background: linear-gradient(135deg, rgba(12,45,62,.34), rgba(25,34,68,.26));
    }
    .sound-cue-event.played, .sound-cue-event.static { border-color: rgba(103,247,177,.30); }
    .sound-cue-event-icon {
      width: 34px;
      height: 34px;
      border-radius: 12px;
      display: grid;
      place-items: center;
      background: rgba(255,255,255,.08);
      border: 1px solid rgba(255,255,255,.10);
      font-size: 17px;
      line-height: 1;
      grid-area: icon;
    }
    .sound-cue-event-main { min-width: 0; display: grid; gap: 2px; grid-area: main; }
    .sound-cue-event-title { min-width: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; color: #e7fbff; font-size: 12px; font-weight: 850; line-height: 1.2; }
    .sound-cue-event-context { color: var(--faint); font-size: 10px; text-transform: uppercase; letter-spacing: .08em; line-height: 1.2; }
    .sound-cue-event-status {
      min-height: 28px;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      gap: 7px;
      padding: 5px 9px;
      border-radius: 999px;
      color: #d9f8ef;
      background: rgba(103,247,177,.08);
      border: 1px solid rgba(103,247,177,.16);
      font-size: 10.5px;
      white-space: nowrap;
      grid-area: status;
    }
    .sound-cue-event-body {
      grid-area: body;
      min-width: 0;
      margin-top: 2px;
      padding: 10px 11px;
      border-radius: 12px;
      border: 1px solid rgba(255,255,255,.075);
      background: rgba(3,7,16,.24);
      color: #dcecff;
    }
    .sound-cue-event-body[hidden] { display: none; }
    .sound-cue-event-body.markdown p:first-child { margin-top: 0; }
    .sound-cue-event-body.markdown p:last-child { margin-bottom: 0; }
    .sound-cue-event-body .thinking-raw { max-height: 220px; }
    .sound-cue-spinner {
      width: 13px;
      height: 13px;
      border-radius: 50%;
      border: 2px solid rgba(255,255,255,.18);
      border-top-color: var(--green);
      animation: soundCueSpin .78s linear infinite;
    }
    .sound-cue-event.played .sound-cue-spinner, .sound-cue-event.static .sound-cue-spinner { display: none; }
    .sound-cue-event.played .sound-cue-event-status::before, .sound-cue-event.static .sound-cue-event-status::before {
      content: "✓";
      color: var(--green);
      font-weight: 900;
    }
    @keyframes soundCueSpin { to { transform: rotate(360deg); } }
    .user .card {
      color: #f7f7ff;
      border-color: rgba(128,167,255,.32);
      background: linear-gradient(135deg, rgba(70, 91, 210, .34), rgba(118, 82, 190, .24));
    }
    .tool .card, .tool-card {
      border-color: rgba(255,209,102,.28);
      background: rgba(70, 50, 14, .22);
    }

    .tool-group {
      margin-top: 13px;
      display: none;
      gap: 10px;
    }
    .tool-group.has-tools { display: grid; }
    .tool-toggle {
      width: fit-content;
      min-height: 30px;
      padding: 5px 10px;
      border-radius: 999px;
      color: var(--amber);
      background: rgba(255,209,102,.08);
      border-color: rgba(255,209,102,.25);
      font-size: 12px;
    }
    .tool-latest, .tool-list { display: grid; gap: 10px; }
    .tool-list { display: none; }
    .tool-group.expanded .tool-list { display: grid; }
    .tool-card {
      border: 1px solid rgba(255,209,102,.24);
      border-radius: 18px;
      padding: 12px;
      color: #f4dda4;
      white-space: pre-wrap;
      background: rgba(70, 50, 14, .18);
    }
    .tool-card.highlight {
      border-color: rgba(103,247,177,.46);
      background: linear-gradient(135deg, rgba(103,247,177,.12), rgba(255,209,102,.10));
      box-shadow: 0 0 0 1px rgba(103,247,177,.10), 0 14px 34px rgba(0,0,0,.22);
    }
    .tool-head { display: flex; justify-content: space-between; gap: 10px; color: var(--amber); font-size: 12px; margin-bottom: 7px; }
    .tool-head span:first-child { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .tool-result { max-height: 170px; overflow: auto; color: #d7c89a; font-size: 12px; line-height: 1.55; }

    .agent-panel { overflow: hidden; }
    .agent-landing {
      height: 100%;
      min-height: 0;
      display: grid;
      grid-template-columns: minmax(280px, var(--unit-fr)) minmax(0, var(--phi-fr));
      gap: 18px;
    }
    .agent-side, .agent-main { border-radius: var(--radius-xl); min-width: 0; }
    .agent-side {
      padding: 18px;
      display: grid;
      align-content: start;
      gap: 14px;
      overflow: auto;
    }
    .agent-main {
      display: grid;
      grid-template-rows: auto minmax(0, 1fr);
      overflow: hidden;
    }
    .agent-hero {
      border: 1px solid var(--line);
      border-radius: 24px;
      padding: 18px;
      background:
        linear-gradient(135deg, rgba(103,247,177,.12), rgba(100,219,255,.06) 48%, rgba(167,139,250,.10)),
        rgba(255,255,255,.035);
    }
    .agent-hero h2 { font-size: 20px; letter-spacing: -.02em; }
    .agent-hero p { margin-top: 8px; color: var(--soft); line-height: 1.65; font-size: 13px; }
    .agent-tabs-reserve { display: grid; gap: 8px; }
    .reserve-title { color: var(--soft); font-size: 12px; text-transform: uppercase; letter-spacing: .08em; }
    .reserve-grid { display: flex; flex-wrap: wrap; gap: 8px; }
    .reserve-chip { padding: 7px 10px; border: 1px dashed rgba(255,255,255,.16); border-radius: 999px; color: var(--faint); background: rgba(255,255,255,.035); font-size: 12px; }
    .agent-status-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 10px; }
    .agent-status-card { border: 1px solid var(--line); border-radius: 16px; padding: 12px; background: rgba(255,255,255,.04); }
    .agent-status-card span { display: block; color: var(--soft); font-size: 11px; text-transform: uppercase; letter-spacing: .05em; }
    .agent-status-card strong { display: block; margin-top: 5px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .agent-main-head {
      padding: 20px 22px;
      border-bottom: 1px solid var(--line);
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: 16px;
      background: linear-gradient(180deg, rgba(255,255,255,.055), rgba(255,255,255,.025));
    }
    .agent-main-head h2 { font-size: 20px; letter-spacing: -.02em; }
    .agent-main-head p { margin-top: 5px; color: var(--soft); font-size: 13px; }
    .agent-main-body { overflow: auto; padding: 22px; display: grid; gap: 18px; }
    .agent-form { display: grid; gap: 18px; }
    .agent-form-grid { display: grid; grid-template-columns: minmax(0, var(--unit-fr)) minmax(0, var(--phi-fr)); gap: 14px; }
    .agent-field.full { grid-column: 1 / -1; }
    .agent-field label { margin-bottom: 7px; }
    .agent-field small { display: block; margin-top: 6px; color: var(--faint); font-size: 12px; line-height: 1.45; }
    .provider-key-link { display: inline-flex; width: fit-content; margin-top: 7px; color: var(--cyan); font-size: 12px; font-weight: 760; text-decoration: none; }
    .provider-key-link:hover { color: var(--green); text-decoration: underline; text-underline-offset: 3px; }
    .provider-key-link[hidden] { display: none !important; }
    .model-combo { position: relative; min-width: 0; z-index: 12; }
    .model-combo input { padding-right: 48px; }
    .model-combo-toggle { position: absolute; top: 5px; right: 5px; width: 34px; min-height: 34px; padding: 0; display: grid; place-items: center; border-radius: 12px; border-color: rgba(255,255,255,.10); background: rgba(255,255,255,.06); }
    .model-combo-arrow { width: 8px; height: 8px; border-right: 2px solid var(--soft); border-bottom: 2px solid var(--soft); transform: translateY(-2px) rotate(45deg); transition: transform var(--motion-fast) var(--ease-smooth), border-color var(--motion-fast) var(--ease-smooth); }
    .model-combo.open .model-combo-arrow { transform: translateY(2px) rotate(225deg); border-color: var(--green); }
    .model-combo-menu { position: absolute; left: 0; right: 0; top: calc(100% + 8px); max-height: 270px; overflow: auto; display: grid; gap: 7px; padding: 8px; border: 1px solid rgba(180,205,255,.18); border-radius: 16px; background: linear-gradient(180deg, rgba(16,25,45,.82), rgba(5,10,22,.70)); box-shadow: 0 24px 60px rgba(0,0,0,.34), inset 0 1px 0 rgba(255,255,255,.08); backdrop-filter: blur(18px); -webkit-backdrop-filter: blur(18px); }
    .model-combo-menu[hidden] { display: none !important; }
    .model-combo-option { min-height: 44px; display: grid; grid-template-columns: minmax(0, 1fr) auto; gap: 8px; align-items: center; padding: 8px 10px; border: 1px solid transparent; border-radius: 12px; color: var(--ink); background: rgba(255,255,255,.035); text-align: left; box-shadow: none; }
    .model-combo-option:hover, .model-combo-option.active { transform: none; border-color: rgba(103,247,177,.28); background: linear-gradient(135deg, rgba(103,247,177,.13), rgba(100,219,255,.08)); filter: none; }
    .model-combo-id { min-width: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; font-family: ui-monospace,SFMono-Regular,Consolas,monospace; font-size: 12px; font-weight: 850; }
    .model-combo-meta { color: var(--faint); font-size: 10.5px; white-space: nowrap; }
    .model-combo-empty { padding: 11px 10px; color: var(--faint); font-size: 12px; text-align: center; }
    .agent-actions { display: flex; justify-content: flex-end; gap: 10px; }
    .agent-actions button { width: auto; min-width: 128px; }
    .agent-paths { display: grid; gap: 10px; }
    .path-row { display: grid; grid-template-columns: 120px minmax(0, 1fr); gap: 10px; align-items: center; color: var(--soft); font-size: 12px; }
    .path-row code { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; padding: 8px 10px; border-radius: 12px; color: #d8fff0; background: rgba(103,247,177,.08); }
    .config-state { color: var(--soft); font-size: 12px; }
    #homePage.active .home-landing,
    #agentTab.active .agent-side, #scheduleTab.active .agent-side, #skillsTab.active .agent-side, #settingsTab.active .agent-side, #labTab.active .agent-side,
    #agentTab.active .agent-main, #scheduleTab.active .agent-main, #skillsTab.active .agent-main, #settingsTab.active .agent-main, #labTab.active .agent-main {
      animation: goldenPanelIn var(--motion-panel) var(--ease-soft) both;
    }
    #agentTab.active .agent-main, #scheduleTab.active .agent-main, #skillsTab.active .agent-main, #settingsTab.active .agent-main, #labTab.active .agent-main { animation-delay: .05s; }
    #agentTab .agent-main, #scheduleTab .agent-main, #skillsTab .agent-main, #settingsTab .agent-main, #labTab .agent-main {
      position: relative;
      isolation: isolate;
    }
    #agentTab .agent-main > *, #scheduleTab .agent-main > *, #skillsTab .agent-main > *, #settingsTab .agent-main > *, #labTab .agent-main > * {
      position: relative;
      z-index: 1;
    }
    #agentTab .agent-main::before, #scheduleTab .agent-main::before, #skillsTab .agent-main::before, #settingsTab .agent-main::before, #labTab .agent-main::before {
      content: "";
      position: absolute;
      inset: 0;
      z-index: 0;
      pointer-events: none;
      opacity: 0;
      background: linear-gradient(110deg, transparent 8%, rgba(255,255,255,.055) 26%, transparent 42%);
      animation: goldenSheen 6.5s var(--ease-smooth) .35s infinite;
    }
    #agentTab.active .agent-hero, #agentTab.active .control-card, #agentTab.active .agent-status-card, #agentTab.active .agent-tabs-reserve,
    #scheduleTab.active .agent-hero, #scheduleTab.active .control-card, #scheduleTab.active .note-card,
    #skillsTab.active .agent-hero, #skillsTab.active .control-card,
    #settingsTab.active .agent-hero, #settingsTab.active .control-card, #settingsTab.active .agent-tabs-reserve,
    #labTab.active .agent-hero, #labTab.active .control-card, #labTab.active .note-card {
      animation: goldenCardIn .34s var(--ease-soft) both;
    }
    #agentTab .agent-hero, #agentTab .control-card, #agentTab .agent-status-card, #agentTab .agent-tabs-reserve,
    #scheduleTab .agent-hero, #scheduleTab .control-card, #scheduleTab .note-card,
    #skillsTab .agent-hero, #skillsTab .control-card,
    #settingsTab .agent-hero, #settingsTab .control-card, #settingsTab .agent-tabs-reserve,
    #labTab .agent-hero, #labTab .control-card, #labTab .note-card {
      transition: transform var(--motion-fast) var(--ease-smooth), border-color var(--motion-fast) var(--ease-smooth), box-shadow var(--motion-fast) var(--ease-smooth), background var(--motion-fast) var(--ease-smooth);
    }
    #agentTab .agent-hero:hover, #agentTab .control-card:hover, #agentTab .agent-status-card:hover, #agentTab .agent-tabs-reserve:hover,
    #scheduleTab .agent-hero:hover, #scheduleTab .control-card:hover, #scheduleTab .note-card:hover,
    #skillsTab .agent-hero:hover, #skillsTab .control-card:hover,
    #settingsTab .agent-hero:hover, #settingsTab .control-card:hover, #settingsTab .agent-tabs-reserve:hover,
    #labTab .agent-hero:hover, #labTab .control-card:hover, #labTab .note-card:hover {
      transform: translateY(-2px);
      border-color: rgba(103,247,177,.30);
      box-shadow: 0 18px 46px rgba(0,0,0,.20), inset 0 1px 0 rgba(255,255,255,.06);
    }
    #agentTab select, #agentTab textarea, #agentTab input,
    #scheduleTab select, #scheduleTab textarea, #scheduleTab input,
    #skillsTab select, #skillsTab textarea, #skillsTab input,
    #settingsTab select, #settingsTab textarea, #settingsTab input,
    #labTab select, #labTab textarea, #labTab input {
      transition: border-color var(--motion-fast) var(--ease-smooth), box-shadow var(--motion-fast) var(--ease-smooth), background var(--motion-fast) var(--ease-smooth);
    }
    #agentTab select:focus, #agentTab textarea:focus, #agentTab input:focus,
    #scheduleTab select:focus, #scheduleTab textarea:focus, #scheduleTab input:focus,
    #skillsTab select:focus, #skillsTab textarea:focus, #skillsTab input:focus,
    #settingsTab select:focus, #settingsTab textarea:focus, #settingsTab input:focus,
    #labTab select:focus, #labTab textarea:focus, #labTab input:focus {
      border-color: rgba(103,247,177,.42);
      box-shadow: 0 0 0 3px rgba(103,247,177,.08), 0 10px 28px rgba(0,0,0,.16);
      background: rgba(5, 11, 22, .74);
    }
    .settings-panel { overflow: hidden; }
    .settings-panel label, .lab-panel label { min-width: 0; gap: 8px; align-items: flex-start; }
    .settings-panel label span, .lab-panel label span { min-width: 0; line-height: 1.25; overflow-wrap: anywhere; }
    .settings-panel label span:last-child, .lab-panel label span:last-child { flex: 0 0 auto; max-width: 42%; overflow: hidden; text-align: right; text-overflow: ellipsis; white-space: nowrap; }
    .settings-landing { height: 100%; min-height: 0; display: grid; grid-template-columns: minmax(258px, var(--unit-fr)) minmax(0, var(--phi-fr)); gap: 16px; }
    .settings-side { padding: 14px; gap: 12px; align-content: start; }
    .settings-panel .agent-hero { border-radius: 20px; padding: 16px; }
    .settings-panel .agent-hero h2 { font-size: clamp(18px, 1.7vw, 22px); }
    .settings-panel .agent-hero p { margin-top: 7px; font-size: 12px; line-height: 1.6; }
    .settings-section-nav { display: grid; gap: 8px; padding: 8px; border: 1px solid var(--line); border-radius: 20px; background: linear-gradient(180deg,rgba(255,255,255,.052),rgba(255,255,255,.026)); box-shadow: inset 0 1px 0 rgba(255,255,255,.04); }
    .settings-tag { width: 100%; min-height: 48px; display: grid; grid-template-columns: 34px minmax(0,1fr); align-items: center; gap: 10px; padding: 8px 10px; border-radius: 14px; text-align: left; color: var(--soft); background: transparent; border-color: transparent; box-shadow: none; }
    .settings-tag:hover:not(:disabled) { transform: translateY(-1px); border-color: rgba(103,247,177,.18); background: rgba(255,255,255,.045); }
    .settings-tag.active { color: var(--ink); border-color: rgba(103,247,177,.34); background: linear-gradient(135deg,rgba(103,247,177,.16),rgba(100,219,255,.10)); box-shadow: inset 0 1px 0 rgba(255,255,255,.06), 0 12px 30px rgba(0,0,0,.16); }
    .settings-tag-icon { width: 34px; height: 34px; display: grid; place-items: center; border-radius: 12px; color: #06130d; font-weight: 900; background: linear-gradient(135deg,var(--green),var(--cyan)); }
    .settings-tag-copy { min-width: 0; display: grid; gap: 2px; }
    .settings-tag-title { font-size: 13px; font-weight: 830; line-height: 1.2; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .settings-tag-sub { color: var(--faint); font-size: 11px; line-height: 1.25; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .settings-status-card { padding: 12px; border: 1px solid var(--line); border-radius: 18px; background: rgba(255,255,255,.035); }
    .settings-status-card .reserve-title { margin-bottom: 8px; }
    .settings-panel .agent-main-head { padding: 16px 19px; }
    .settings-body { min-height: 0; overflow: auto; padding: 16px; display: block; }
    .settings-section-view { display: none; min-height: 100%; }
    .settings-section-view.active { display: block; animation: goldenCardIn .24s var(--ease-soft) both; }
    .settings-section-grid { display: grid; grid-template-columns: minmax(228px, var(--unit-fr)) minmax(0, var(--phi-fr)); gap: 14px; align-items: stretch; }
    .settings-card { min-width: 0; border-radius: 18px; padding: 15px; background: linear-gradient(180deg,rgba(255,255,255,.052),rgba(255,255,255,.026)); box-shadow: inset 0 1px 0 rgba(255,255,255,.045); }
    .settings-language-card { min-height: 170px; display: grid; grid-template-rows: 1fr auto; gap: 14px; align-content: space-between; }
    .settings-skill-validation-card { grid-column: 1 / -1; display: grid; grid-template-columns: 1fr; gap: 14px; align-items: start; }
    .settings-skill-validation-controls { align-self: start; }
    .settings-events-card { grid-column: 1 / -1; min-height: 354px; display: grid; grid-template-rows: auto auto auto minmax(0, .7fr) minmax(0, 1.3fr); gap: 12px; align-content: stretch; }
    .settings-events-card .settings-save-row { margin-top: 0; }
    .settings-events-row { display: grid; grid-template-columns: minmax(210px, 1fr) auto; gap: 10px; align-items: end; }
    .settings-events-row .field { margin: 0; min-width: 0; }
    .settings-events-row button { width: auto; min-width: 88px; min-height: 34px; padding: 6px 12px; border-radius: 11px; justify-self: end; font-size: 12px; }
    .settings-events-card .memory-list { min-height: 0; overflow: auto; }
    .settings-memory-layout { display: grid; grid-template-columns: minmax(220px, var(--unit-fr)) minmax(0, var(--phi-fr)); gap: 14px; align-items: stretch; }
    .settings-memory-card { min-height: 210px; display: grid; align-content: start; gap: 14px; overflow: hidden; }
    .settings-memory-editor { display: grid; grid-template-rows: auto 1fr auto; gap: 14px; }
    .settings-card-copy { display: grid; gap: 6px; min-width: 0; align-content: start; }
    .settings-card-copy strong { font-size: 15px; line-height: 1.25; }
    .settings-card-copy p { margin: 0; color: var(--soft); font-size: 12px; line-height: 1.55; }
    .settings-memory-meter { display: grid; gap: 10px; margin-top: 2px; }
    .settings-memory-meter > span { display: flex; align-items: center; justify-content: space-between; gap: 12px; padding: 9px 10px; border: 1px solid rgba(255,255,255,.075); border-radius: 13px; color: var(--soft); background: rgba(3,7,16,.22); font-size: 12px; }
    .settings-memory-meter b { color: var(--ink); font-family: ui-monospace,SFMono-Regular,Consolas,monospace; font-size: 11px; }
    .settings-lang-button { width: auto; min-width: 96px; min-height: 38px; border-radius: 12px; justify-self: start; }
    .settings-memory-limits { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 12px; margin: 0; min-width: 0; }
    .settings-memory-limits .field { margin: 0; min-width: 0; padding: 12px; border: 1px solid rgba(255,255,255,.075); border-radius: 15px; background: rgba(3,7,16,.24); }
    .settings-memory-limits .field label { display: grid; grid-template-columns: minmax(0, 1fr) auto; align-items: center; gap: 8px; margin-bottom: 8px; }
    .settings-memory-limits .field label span:last-child { max-width: none; padding: 3px 7px; border-radius: 999px; background: rgba(100,219,255,.08); color: var(--faint); font-size: 10px; text-transform: uppercase; letter-spacing: .04em; }
    .settings-memory-limits input, .settings-memory-limits select { width: 100%; min-height: 42px; border-radius: 12px; font-family: ui-monospace,SFMono-Regular,Consolas,monospace; font-size: 13px; }
    .settings-save-row { display: flex; justify-content: flex-start; margin-top: -2px; }
    .settings-save-row button { width: auto; min-width: 132px; min-height: 38px; border-radius: 12px; }
    .settings-sound-layout { display: grid; grid-template-columns: 1fr; gap: 14px; align-items: start; }
    .settings-sound-master { display: grid; gap: 14px; align-content: start; }
    .sound-cue-master-controls { display: grid; grid-template-columns: minmax(0, var(--unit-fr)) minmax(0, var(--phi-fr)); gap: 12px; align-items: start; }
    .sound-cue-toggle { min-width: 0; display: flex; align-items: center; justify-content: space-between; gap: 12px; padding: 10px 12px; border: 1px solid rgba(255,255,255,.10); border-radius: 14px; background: rgba(3,7,16,.24); color: var(--soft); font-size: 12px; }
    .sound-cue-toggle input { width: 18px; height: 18px; accent-color: var(--green); flex: 0 0 auto; }
    .sound-cue-toggle span, .sound-cue-toggle span:last-child { flex: 1 1 auto; max-width: none; overflow: visible; text-align: left; white-space: normal; }
    .sound-cue-volume, .sound-cue-delay { display: grid; gap: 7px; }
    .sound-cue-volume input, .sound-cue-delay input { width: 100%; height: 28px; accent-color: var(--cyan); }
    .sound-cue-volume input[type="range"], .sound-cue-delay input[type="range"] { -webkit-appearance: none; appearance: none; background: transparent; cursor: pointer; }
    .sound-cue-volume input[type="range"]::-webkit-slider-runnable-track, .sound-cue-delay input[type="range"]::-webkit-slider-runnable-track { height: 6px; border-radius: 999px; background: linear-gradient(90deg, rgba(100,219,255,.78), rgba(103,247,177,.64)); box-shadow: inset 0 0 0 1px rgba(255,255,255,.12); }
    .sound-cue-volume input[type="range"]::-webkit-slider-thumb, .sound-cue-delay input[type="range"]::-webkit-slider-thumb { -webkit-appearance: none; appearance: none; width: 18px; height: 18px; margin-top: -6px; border-radius: 50%; border: 2px solid rgba(5,11,22,.94); background: #e7fbff; box-shadow: 0 3px 12px rgba(0,0,0,.35); }
    .sound-cue-volume input[type="range"]::-moz-range-track, .sound-cue-delay input[type="range"]::-moz-range-track { height: 6px; border: 0; border-radius: 999px; background: linear-gradient(90deg, rgba(100,219,255,.78), rgba(103,247,177,.64)); box-shadow: inset 0 0 0 1px rgba(255,255,255,.12); }
    .sound-cue-volume input[type="range"]::-moz-range-thumb, .sound-cue-delay input[type="range"]::-moz-range-thumb { width: 16px; height: 16px; border-radius: 50%; border: 2px solid rgba(5,11,22,.94); background: #e7fbff; box-shadow: 0 3px 12px rgba(0,0,0,.35); }
    .sound-cue-list-card { display: grid; grid-template-columns: minmax(0, 1fr) auto; gap: 12px; min-height: 100%; align-items: start; }
    .sound-cue-library-actions { display: inline-flex; align-items: center; justify-content: flex-end; gap: 8px; align-self: start; }
    .sound-cue-library-actions button { width: auto; min-height: 34px; border-radius: 11px; padding: 7px 12px; }
    .sound-cue-list { min-width: 0; }
    .sound-cue-list { grid-column: 1 / -1; }
    .sound-cue-board { display: grid; grid-template-columns: 1fr; gap: 12px; align-items: start; min-height: clamp(380px, 48vh, 560px); }
    .sound-cue-group-nav { min-width: 0; display: flex; gap: 8px; padding: 8px; border: 1px solid rgba(255,255,255,.08); border-radius: 18px; background: rgba(3,7,16,.18); overflow-x: auto; }
    .sound-cue-group-tab { flex: 0 0 clamp(132px, 18%, 180px); min-height: 48px; display: grid; grid-template-columns: minmax(0, 1fr) auto; gap: 8px; align-items: center; padding: 9px 10px; border-radius: 13px; color: var(--soft); background: transparent; border-color: transparent; box-shadow: none; text-align: left; }
    .sound-cue-group-tab:hover:not(:disabled) { transform: translateY(-1px); border-color: rgba(103,247,177,.18); background: rgba(255,255,255,.045); }
    .sound-cue-group-tab.active { color: var(--ink); border-color: rgba(103,247,177,.32); background: linear-gradient(135deg, rgba(103,247,177,.13), rgba(100,219,255,.08)); box-shadow: inset 0 1px 0 rgba(255,255,255,.055); }
    .sound-cue-group-tab-title { min-width: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; font-size: 12px; font-weight: 830; line-height: 1.2; }
    .sound-cue-group-tab-count { min-width: 22px; min-height: 22px; display: grid; place-items: center; padding: 0 6px; border-radius: 999px; color: var(--faint); background: rgba(255,255,255,.06); font-size: 10px; font-family: ui-monospace,SFMono-Regular,Consolas,monospace; }
    .sound-cue-group-panel { min-width: 0; min-height: clamp(300px, 38vh, 470px); display: grid; gap: 10px; align-content: start; padding: 12px; border: 1px solid rgba(255,255,255,.085); border-radius: 20px; background: linear-gradient(135deg, rgba(255,255,255,.038), rgba(3,7,16,.20)); }
    .sound-cue-group-head { display: grid; grid-template-columns: minmax(0,var(--phi-fr)) minmax(92px,var(--unit-fr)); gap: 10px; align-items: end; }
    .sound-cue-group-title { color: var(--ink); font-size: 13px; font-weight: 860; line-height: 1.25; }
    .sound-cue-group-desc { color: var(--soft); font-size: 11px; line-height: 1.45; }
    .sound-cue-group-count { justify-self: end; color: var(--faint); font-size: 11px; white-space: nowrap; }
    .sound-cue-group-body { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 10px; align-items: start; }
    .sound-cue-row { display: grid; grid-template-columns: 1fr; gap: 12px; align-items: start; padding: 13px; border: 1px solid rgba(255,255,255,.10); border-radius: 18px; background: linear-gradient(135deg, rgba(255,255,255,.045), rgba(3,7,16,.22)); box-shadow: inset 0 1px 0 rgba(255,255,255,.035); }
    .sound-cue-row.off { opacity: .58; }
    .sound-cue-head { min-width: 0; display: grid; gap: 9px; align-content: start; }
    .sound-cue-title { color: var(--ink); font-size: 13px; font-weight: 850; line-height: 1.25; }
    .sound-cue-desc { color: var(--soft); font-size: 11px; line-height: 1.45; }
    .sound-cue-body { min-width: 0; display: grid; grid-template-columns: 1fr; grid-template-areas: "actions" "assets"; gap: 10px; align-items: start; }
    .sound-cue-actions { grid-area: actions; display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 8px; align-content: start; }
    .sound-cue-actions button { width: 100%; min-height: 34px; border-radius: 11px; padding: 7px 10px; }
    .sound-cue-assets { display: grid; gap: 7px; min-width: 0; }
    .sound-cue-assets { grid-area: assets; }
    .sound-cue-asset { max-width: 100%; display: grid; grid-template-columns: 58px minmax(0, 1fr) 114px; align-items: center; gap: 8px; min-height: 40px; padding: 6px 7px 6px 9px; border: 1px solid rgba(100,219,255,.16); border-radius: 13px; background: rgba(100,219,255,.06); color: #dff8ff; font-size: 11px; }
    .sound-cue-asset.custom { border-color: rgba(103,247,177,.22); background: rgba(103,247,177,.08); color: #dfffee; }
    .sound-cue-asset.disabled { opacity: .52; filter: saturate(.65); }
    .sound-cue-source { display: inline-flex; align-items: center; justify-content: center; min-width: 44px; min-height: 22px; padding: 3px 7px; border-radius: 999px; background: rgba(255,255,255,.065); color: var(--faint); font-size: 10px; text-transform: uppercase; }
    .sound-cue-asset-name { min-width: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .sound-cue-asset-actions { display: inline-flex; align-items: center; justify-content: flex-end; gap: 5px; }
    .sound-cue-asset-toggle { width: 78px; display: inline-flex; align-items: center; justify-content: center; gap: 5px; min-height: 28px; padding: 0 7px; border: 1px solid rgba(255,255,255,.10); border-radius: 10px; background: rgba(255,255,255,.055); color: var(--soft); font-size: 10px; line-height: 1; cursor: pointer; }
    .sound-cue-asset-toggle input[type="checkbox"] { width: 14px; height: 14px; margin: 0; accent-color: var(--green); flex: 0 0 auto; }
    .sound-cue-asset-toggle span, .sound-cue-asset-toggle span:last-child { flex: 0 0 auto; max-width: none; overflow: visible; text-align: left; white-space: nowrap; }
    .sound-cue-asset-toggle.disabled-toggle { opacity: .46; cursor: not-allowed; }
    .sound-cue-asset button { width: 28px; height: 28px; min-height: 28px; padding: 0; border-radius: 10px; display: grid; place-items: center; color: var(--soft); background: rgba(255,255,255,.06); }
    .sound-cue-asset button:hover:not(:disabled) { color: #06130d; background: linear-gradient(135deg,var(--green),var(--cyan)); }
    .sound-cue-asset button:disabled { opacity: .38; cursor: not-allowed; }
    .sound-cue-empty { color: var(--faint); font-size: 12px; line-height: 1.5; }
    .sound-cue-custom-panel { display: grid; gap: 12px; }
    .sound-cue-custom-form { display: grid; grid-template-columns: minmax(0, var(--unit-fr)) minmax(0, var(--phi-fr)) auto; gap: 9px; padding: 10px; border: 1px solid rgba(255,255,255,.085); border-radius: 16px; background: rgba(3,7,16,.22); }
    .sound-cue-custom-form input { width: 100%; min-height: 38px; border-radius: 11px; }
    .sound-cue-custom-form button { width: auto; min-height: 38px; border-radius: 11px; padding: 7px 13px; }
    .sound-cue-custom-edit { display: grid; grid-template-columns: minmax(0, var(--unit-fr)) minmax(0, var(--phi-fr)); gap: 8px; }
    .sound-cue-custom-edit input { width: 100%; min-height: 34px; border-radius: 10px; font-size: 12px; }
    .sound-cue-custom-buttons { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 8px; }
    .sound-cue-custom-buttons button { width: 100%; min-height: 34px; border-radius: 11px; padding: 7px 8px; }
    .sound-cue-custom-empty { grid-column: 1 / -1; padding: 18px 12px; border: 1px dashed rgba(255,255,255,.14); border-radius: 16px; color: var(--faint); text-align: center; font-size: 12px; line-height: 1.5; background: rgba(3,7,16,.16); }
    .multimodal-card { min-height: 100%; }
    @media (max-width: 1180px) {
      .settings-landing { grid-template-columns: 1fr; }
      .settings-side { grid-template-columns: minmax(0, 1fr) minmax(260px, var(--unit-fr)); align-items: stretch; }
      .settings-section-nav { align-self: stretch; }
      .settings-status-card { grid-column: 1 / -1; }
    }
    @media (max-width: 920px) {
      .settings-section-grid, .settings-memory-layout, .settings-memory-card, .settings-skill-validation-card, .settings-sound-layout, .settings-events-row, .sound-cue-row, .sound-cue-group-head, .sound-cue-board, .sound-cue-master-controls, .sound-cue-group-body, .sound-cue-custom-form, .sound-cue-custom-edit { grid-template-columns: 1fr; }
      .sound-cue-group-count { justify-self: start; }
      .settings-save-row { grid-column: 1; }
      .settings-language-card { min-height: auto; }
    }
    @media (max-width: 640px) {
      .settings-body { padding: 12px; gap: 10px; }
      .settings-side { grid-template-columns: 1fr; }
      .settings-tag { min-height: 44px; }
      .settings-lang-button, .settings-save-row button { width: 100%; }
      .settings-events-row button { justify-self: stretch; }
      .settings-memory-limits { grid-template-columns: 1fr; }
      .sound-cue-group-nav, .sound-cue-actions { grid-template-columns: 1fr; }
      .sound-cue-asset { grid-template-columns: 56px minmax(0, 1fr); grid-template-areas: "source name" "actions actions"; }
      .sound-cue-source { grid-area: source; }
      .sound-cue-asset-name { grid-area: name; }
      .sound-cue-asset-actions { grid-area: actions; justify-content: flex-start; }
      .chat-jump-bottom { right: 16px; bottom: 104px; }
    }
    .schedule-body textarea#scheduleJson { min-height: 120px; border: 1px solid var(--line); background: rgba(3, 7, 16, .62); }
    .schedule-list-head { display: flex; align-items: center; justify-content: space-between; gap: 12px; }
    .schedule-form small { display: block; margin-top: 6px; color: var(--faint); font-size: 12px; }

    .composer {
      padding: 17px 20px 20px;
      border-top: 1px solid var(--line);
      background: linear-gradient(180deg, rgba(255,255,255,.035), rgba(5,8,16,.52));
    }
    .composer.read-only .composer-box {
      border-color: rgba(100,219,255,.20);
      background: rgba(5, 9, 18, .56);
    }
    .composer.read-only textarea {
      color: var(--faint);
    }
    .composer-box {
      display: grid;
      gap: 11px;
      border: 1px solid rgba(255,255,255,.13);
      border-radius: 24px;
      padding: 12px;
      background: rgba(5, 9, 18, .72);
      box-shadow: inset 0 1px 0 rgba(255,255,255,.04);
    }
    textarea {
      min-height: 74px;
      max-height: 190px;
      resize: vertical;
      border: 0;
      border-radius: 16px;
      padding: 6px 8px;
      line-height: 1.55;
      background: transparent;
    }
    textarea::placeholder { color: #6f7c94; }
    .composer-actions { display: flex; align-items: center; justify-content: space-between; gap: 14px; }
    .composer-buttons { display: flex; align-items: center; gap: 10px; }
    .stop { width: auto; min-width: 96px; border-radius: 16px; }
    .hint { color: var(--soft); font-size: 12px; }
    .file-zone { min-width: 0; flex: 1 1 auto; display: flex; align-items: center; gap: 8px; overflow: hidden; }
    .attach-btn { width: 34px!important; min-width: 34px; height: 34px; min-height: 34px; padding: 0; border-radius: 13px; display: grid; place-items: center; font-size: 20px; line-height: 1; color: var(--green); }
    .chat-attachment-input { display: none; }
    .attachment-strip { min-width: 0; display: flex; align-items: center; gap: 8px; overflow-x: auto; scrollbar-width: thin; }
    .attachment-empty { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; color: var(--soft); font-size: 12px; }
    .attachment-chip { flex: 0 0 auto; max-width: 190px; display: grid; grid-template-columns: 28px minmax(0,1fr) 22px; align-items: center; gap: 7px; padding: 5px 6px; border: 1px solid rgba(255,255,255,.12); border-radius: 12px; background: rgba(255,255,255,.045); }
    .attachment-chip img { width: 28px; height: 28px; border-radius: 8px; object-fit: cover; background: rgba(255,255,255,.06); }
    .attachment-icon { width: 28px; height: 28px; border-radius: 8px; display: grid; place-items: center; color: #06130d; background: linear-gradient(135deg,var(--green),var(--cyan)); font-size: 13px; font-weight: 900; }
    .attachment-copy { min-width: 0; display: grid; gap: 1px; }
    .attachment-name { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; color: var(--ink); font-size: 11px; font-weight: 760; }
    .attachment-meta { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; color: var(--faint); font-size: 10px; }
    .attachment-remove { width: 22px!important; min-width: 22px; height: 22px; min-height: 22px; padding: 0; border-radius: 8px; font-size: 13px; color: var(--soft); }
    .message-attachments { margin-top: 10px; display: flex; flex-wrap: wrap; gap: 8px; }
    .message-attachment { max-width: 230px; display: grid; grid-template-columns: 34px minmax(0,1fr); gap: 8px; align-items: center; padding: 7px; border-radius: 12px; border: 1px solid rgba(255,255,255,.10); background: rgba(255,255,255,.035); }
    .message-attachment img { width: 34px; height: 34px; border-radius: 9px; object-fit: cover; }
    .send { width: auto; min-width: 138px; border-radius: 16px; }
    .voice-hold{display:none;width:100%;min-height:78px;border:1px solid rgba(100,219,255,.18);border-radius:18px;background:linear-gradient(180deg,rgba(12,22,40,.54),rgba(5,9,18,.42));color:var(--ink);font-size:14px;font-weight:760;letter-spacing:0;grid-template-columns:36px minmax(0,1fr);align-items:center;gap:12px;padding:0 16px;text-align:left;touch-action:none;user-select:none;position:relative;overflow:hidden;box-shadow:inset 0 1px 0 rgba(255,255,255,.045);transition:border-color .16s var(--ease-smooth),background .16s var(--ease-smooth),box-shadow .16s var(--ease-smooth),transform .16s var(--ease-smooth);}
    .voice-hold-icon{width:34px;height:34px;display:grid;place-items:center;border-radius:14px;border:1px solid rgba(103,247,177,.30);background:linear-gradient(135deg,rgba(103,247,177,.18),rgba(100,219,255,.10));box-shadow:0 0 22px rgba(100,219,255,.10);}
    .voice-hold-icon svg{width:18px;height:18px;display:block;stroke:currentColor;fill:none;stroke-width:2;stroke-linecap:round;stroke-linejoin:round;}
    .voice-hold:hover:not(:disabled){border-color:rgba(103,247,177,.36);background:linear-gradient(180deg,rgba(16,30,50,.66),rgba(6,12,23,.48));box-shadow:0 12px 30px rgba(0,0,0,.16),inset 0 1px 0 rgba(255,255,255,.06);filter:none;transform:none;}
    .voice-hold.recording{border-color:rgba(103,247,177,.48);box-shadow:0 0 0 3px rgba(103,247,177,.08),inset 0 1px 0 rgba(255,255,255,.06);}
    .voice-hold.recording .voice-hold-icon{animation:voiceHoldPulse 1s ease-in-out infinite;}
    .voice-hold-copy{min-width:0;}
    .voice-hold-title{display:block;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;}
    .voice-hold small{display:block;margin-top:3px;color:var(--faint);font-size:11px;font-weight:520;}
    .composer-box.voice-mode textarea{display:none;}
    .composer-box.voice-mode .voice-hold{display:grid;}
    .composer-box.voice-mode{border-color:rgba(103,247,177,.30);background:rgba(5,9,18,.74);box-shadow:0 0 0 3px rgba(103,247,177,.055),inset 0 1px 0 rgba(255,255,255,.05);}
    .mic{width:42px!important;min-width:42px;flex:0 0 42px;min-height:42px;height:42px;padding:0;border-radius:14px;font-size:0;display:grid;place-items:center;color:var(--soft);border-color:rgba(180,205,255,.16);background:rgba(255,255,255,.035);box-shadow:inset 0 1px 0 rgba(255,255,255,.055);transition:transform .16s var(--ease-smooth),border-color .16s var(--ease-smooth),background .16s var(--ease-smooth),color .16s var(--ease-smooth),box-shadow .16s var(--ease-smooth);}
    .mic svg{width:19px;height:19px;display:block;stroke:currentColor;fill:none;stroke-width:2;stroke-linecap:round;stroke-linejoin:round;}
    .mic:hover:not(:disabled){color:var(--ink);border-color:rgba(100,219,255,.32);background:rgba(100,219,255,.075);filter:none;box-shadow:0 10px 24px rgba(0,0,0,.16),inset 0 1px 0 rgba(255,255,255,.07);}
    .mic.active{color:#06130d;border-color:rgba(103,247,177,.62);background:linear-gradient(135deg,var(--green),var(--cyan));box-shadow:0 12px 28px rgba(100,219,255,.18),inset 0 1px 0 rgba(255,255,255,.24);}
    .voice-record-overlay{position:fixed;inset:0;z-index:1100;display:none;place-items:center;background:rgba(5,7,17,.72);backdrop-filter:blur(10px);}
    .voice-record-overlay.active{display:grid;}
    .voice-record-card{width:min(360px,calc(100vw - 32px));padding:26px;border:1px solid rgba(100,219,255,.24);border-radius:24px;background:linear-gradient(180deg,rgba(14,22,40,.94),rgba(6,10,20,.92));box-shadow:0 24px 80px rgba(0,0,0,.42);text-align:center;}
    .voice-record-time{font:800 34px/1 ui-monospace,SFMono-Regular,Consolas,monospace;color:var(--ink);}
    .voice-record-hint{margin-top:10px;color:var(--soft);font-size:13px;}
    .voice-record-overlay.cancel .voice-record-card{border-color:rgba(255,107,138,.42);}
    .voice-record-overlay.cancel .voice-record-hint,.voice-record-overlay.cancel .voice-record-time{color:#ff9caf;}
    .voice-wave{height:54px;margin:18px auto 4px;display:flex;align-items:center;justify-content:center;gap:5px;}
    .voice-wave span{width:5px;height:16px;border-radius:999px;background:var(--cyan);opacity:.88;animation:voiceWave .72s ease-in-out infinite;}
    .voice-wave span:nth-child(2){animation-delay:.08s;height:26px;background:var(--green);}
    .voice-wave span:nth-child(3){animation-delay:.16s;height:38px;}
    .voice-wave span:nth-child(4){animation-delay:.24s;height:28px;background:var(--green);}
    .voice-wave span:nth-child(5){animation-delay:.32s;height:18px;}
    .voice-record-overlay.cancel .voice-wave span{background:#ff7a96;}
    @keyframes voiceWave{0%,100%{transform:scaleY(.55);opacity:.55;}50%{transform:scaleY(1.2);opacity:1;}}
    @keyframes voiceHoldPulse{0%,100%{box-shadow:0 0 0 0 rgba(103,247,177,.18);}50%{box-shadow:0 0 0 8px rgba(103,247,177,0);}}
    .matrix { display: inline-block; width: 1.1em; color: var(--green); text-shadow: 0 0 18px var(--green); }
    .command-menu {
      display: none;
      max-height: 230px;
      overflow: auto;
      gap: 7px;
      padding: 8px;
      border: 1px solid rgba(100,219,255,.20);
      border-radius: 18px;
      background: linear-gradient(180deg, rgba(12,18,34,.96), rgba(5,9,18,.94));
      box-shadow: 0 18px 48px rgba(0,0,0,.32), inset 0 1px 0 rgba(255,255,255,.05);
    }
    .command-menu.active { display: grid; }
    .command-menu[hidden] { display: none !important; }
    .command-item {
      min-height: 0;
      width: 100%;
      display: grid;
      grid-template-columns: minmax(110px, auto) minmax(0,1fr);
      gap: 10px;
      align-items: center;
      padding: 9px 10px;
      border-radius: 13px;
      color: var(--soft);
      text-align: left;
      background: transparent;
    }
    .command-item.active { color: var(--ink); border-color: rgba(103,247,177,.42); background: rgba(103,247,177,.10); }
    .command-name { color: var(--cyan); font-weight: 850; }
    .command-desc { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; font-size: 12px; }
    @keyframes spin { to { transform: rotate(360deg); } }

    .blank-page {
      position: fixed;
      inset: 0;
      z-index: 20;
      display: none;
      place-items: center;
      background: #02040a;
      color: var(--ink);
    }
    .blank-page.active { display: grid; }
    .blank-card {
      width: min(460px, calc(100vw - 40px));
      padding: 28px;
      border: 1px solid var(--line);
      border-radius: 28px;
      text-align: center;
      background: rgba(255,255,255,.04);
      box-shadow: var(--shadow);
    }
    .blank-card p { margin: 10px 0 18px; color: var(--soft); line-height: 1.6; }

    @media (max-width: 980px) {
      body { overflow: auto; }
      .app-scale-stage { width: 100%; height: auto; min-height: 100vh; }
      .home-shell { position: relative; width: auto; height: auto; min-height: 100vh; padding: 14px; transform: none; }
      .home-landing { min-height: calc(100vh - 28px); border-radius: 24px; }
      .home-ui { padding: 22px; }
      .home-header, .home-hero { grid-template-columns: 1fr; }
      .tab-bar { justify-content: flex-start; }
      .planet-hud { min-width: 0; width: min(440px, 100%); }
      .home-help { align-items: flex-start; flex-direction: column; }
      .planet-dock { justify-content: flex-start; }
      .app, .agent-landing { height: auto; min-height: 100vh; grid-template-columns: 1fr; }
      .stage, .agent-main { min-height: 76vh; }
      .agent-form-grid, .agent-status-grid { grid-template-columns: 1fr; }
      .message, .message.user { width: 100%; }
    }
    /* ===== Schedule Panel (v2: responsive + history styles) ===== */
    .schedule-panel { overflow: hidden; }
    .schedule-landing { height: 100%; min-height: 0; display: grid; grid-template-columns: minmax(280px, var(--unit-fr)) minmax(0, var(--phi-fr)); gap: 16px; }
    .schedule-panel .agent-side { padding: 14px; gap: 12px; }
    .schedule-panel .agent-hero { border-radius: 24px; padding: 18px; }
    .schedule-panel .agent-hero::before { width: 48px; height: 48px; border-radius: 18px; font-size: 22px; margin-bottom: 14px; }
    .schedule-panel .agent-hero h2 { font-size: clamp(18px, 2vw, 24px); margin: 0; letter-spacing: -.03em; }
    .schedule-panel .agent-hero p { margin-top: 8px; color: var(--soft); line-height: 1.65; font-size: clamp(11px, 1.2vw, 13px); }
    .schedule-panel .agent-side .control-card, .schedule-panel .agent-side .note-card { border-radius: 22px; }
    .schedule-panel .agent-main-head { padding: 16px 18px; }
    .schedule-panel .agent-main-body { min-height: 0; overflow: auto; padding: 16px; display: grid; grid-template-columns: minmax(0, var(--phi-fr)) minmax(280px, var(--unit-fr)); gap: 14px; align-items: start; }
    .schedule-panel .schedule-form, .schedule-panel .schedule-history-card { border: 1px solid var(--line); border-radius: 22px; padding: 16px; background: rgba(255,255,255,.045); box-shadow: inset 0 1px 0 rgba(255,255,255,.045); }
    .schedule-panel .schedule-list-card { padding: 14px; border-radius: 22px; background: rgba(255,255,255,.045); }
    .schedule-panel .schedule-form::before { content: var(--schedule-task-spec-label, "TASK SPEC"); display: block; margin-bottom: 10px; color: var(--green); font-size: 10px; font-weight: 900; letter-spacing: .14em; }
    .schedule-panel .schedule-form .agent-form-grid { display: grid; grid-template-columns: minmax(0, var(--phi-fr)) minmax(150px, var(--unit-fr)); gap: 12px; }
    .schedule-panel .schedule-form .full { grid-column: 1 / -1; }
    .schedule-panel .schedule-form textarea { border: 1px solid var(--line); background: rgba(3,7,16,.62); border-radius: 14px; padding: 10px 12px; }
    .schedule-panel #scheduleContent { min-height: 120px; }
    .schedule-panel #scheduleJson { min-height: 140px; font-family: ui-monospace, SFMono-Regular, Consolas, monospace; font-size: 12px; }
    .schedule-panel .schedule-form .agent-actions { justify-content: flex-start; margin-top: 2px; gap: 10px; }
    .schedule-panel .schedule-form .agent-actions button { min-width: 140px; }
    .schedule-panel .schedule-list-card::before { content: var(--schedule-queue-label, "QUEUE"); display: block; margin-bottom: 8px; color: var(--green); font-size: 10px; font-weight: 900; letter-spacing: .14em; }
    .schedule-panel .schedule-history-card::before { content: var(--schedule-trace-label, "TRACE"); display: block; margin-bottom: 8px; color: var(--green); font-size: 10px; font-weight: 900; letter-spacing: .14em; }

    .schedule-list { display: grid; gap: 10px; margin: 10px 0; }
    .schedule-item { position: relative; overflow: hidden; border: 1px solid rgba(255,255,255,.10); border-radius: 18px; padding: 14px 16px; background: linear-gradient(135deg, rgba(255,255,255,.055), rgba(255,255,255,.025)); transition: transform .16s var(--ease-smooth), border-color .16s var(--ease-smooth), box-shadow .16s var(--ease-smooth); animation: goldenCardIn .26s var(--ease-soft) both; display: flex; flex-direction: column; gap: 10px; }
    .schedule-item:hover { transform: translateY(-1px); border-color: rgba(103,247,177,.32); box-shadow: 0 8px 24px rgba(0,0,0,.18); }
    .schedule-item::before { content: ""; position: absolute; inset: 0 auto 0 0; width: 3px; background: linear-gradient(var(--green), var(--cyan)); opacity: .7; border-radius: 0 3px 3px 0; }

    .schedule-item-header { display: flex; align-items: flex-start; justify-content: space-between; gap: 10px; min-width: 0; }
    .schedule-item-header .schedule-title-block { min-width: 0; flex: 1; }
    .schedule-item strong { display: block; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; font-size: clamp(13px, 1.4vw, 15px); color: var(--ink); }
    .schedule-meta { margin-top: 4px; color: var(--soft); font-size: clamp(11px, 1.2vw, 13px); line-height: 1.5; }
    .schedule-badge { display: inline-flex; align-items: center; gap: 5px; padding: 3px 10px; border-radius: 999px; border: 1px solid; font-size: clamp(9px, 1vw, 11px); font-weight: 700; letter-spacing: .02em; margin-bottom: 6px; }
    .schedule-badge.enabled { border-color: rgba(103,247,177,.35); background: rgba(103,247,177,.10); color: #d9fff0; }
    .schedule-badge.paused { border-color: rgba(255,209,102,.34); background: rgba(255,209,102,.10); color: #ffe4a3; }
    .schedule-badge.completed { border-color: rgba(100,219,255,.30); background: rgba(100,219,255,.10); color: #dff7ff; }
    .schedule-badge.failed { border-color: rgba(255,107,138,.35); background: rgba(255,107,138,.10); color: #ffd7df; }

    .schedule-preview { margin: 0; color: #d8e6fa; font-size: clamp(11px, 1.2vw, 13px); line-height: 1.6; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; opacity: .85; }
    .schedule-meta-grid { display: grid; grid-template-columns: 1fr; gap: 6px; }
    .schedule-meta-cell { padding: 8px 10px; border: 1px solid rgba(255,255,255,.09); border-radius: 12px; background: rgba(3,7,16,.30); }
    .schedule-meta-cell span { display: block; color: var(--faint); font-size: 9px; text-transform: uppercase; letter-spacing: .06em; }
    .schedule-meta-cell b { display: block; margin-top: 3px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; color: var(--ink); font-size: clamp(11px, 1.2vw, 13px); }

    .schedule-item-actions { display: flex; gap: 6px; flex-wrap: wrap; justify-content: flex-end; }
    .schedule-item-actions button { width: auto; min-height: 30px; padding: 5px 12px; border-radius: 999px; font-size: clamp(11px, 1.1vw, 12px); white-space: nowrap; }
    .schedule-item-actions .danger { color: #ffd7df; border-color: rgba(255,107,138,.30); background: rgba(255,107,138,.08); }

    /* History entries (previously unstyled!) */
    .schedule-history { max-height: min(50vh, 420px); overflow: auto; margin-top: 10px; padding: 12px; border: 1px solid rgba(255,255,255,.10); border-radius: 18px; color: #d5e3f7; background: rgba(3,7,16,.38); font-size: clamp(10px, 1.1vw, 12px); line-height: 1.6; }
    .schedule-history-entry { padding: 10px 12px; margin-bottom: 8px; border: 1px solid rgba(255,255,255,.08); border-radius: 14px; background: rgba(255,255,255,.04); transition: background .15s ease, transform .15s ease; animation: goldenCardIn .24s var(--ease-soft) both; }
    .schedule-history-entry:last-child { margin-bottom: 0; }
    .schedule-history-entry:hover { background: rgba(255,255,255,.07); transform: translateY(-1px); }
    .schedule-history-head { display: flex; align-items: center; justify-content: space-between; gap: 10px; margin-bottom: 6px; }
    .schedule-history-status { display: inline-flex; align-items: center; gap: 5px; padding: 3px 10px; border-radius: 999px; font-size: clamp(9px, 1vw, 11px); font-weight: 800; text-transform: uppercase; letter-spacing: .04em; }
    .schedule-history-status::before { content: ""; width: 6px; height: 6px; border-radius: 50%; display: inline-block; }
    .schedule-history-status.success { border: 1px solid rgba(103,247,177,.30); background: rgba(103,247,177,.10); color: #d9fff0; }
    .schedule-history-status.success::before { background: var(--green); box-shadow: 0 0 6px var(--green); }
    .schedule-history-status.error { border: 1px solid rgba(255,107,138,.30); background: rgba(255,107,138,.10); color: #ffd7df; }
    .schedule-history-status.error::before { background: var(--rose); box-shadow: 0 0 6px var(--rose); }
    .schedule-history-status.running { border: 1px solid rgba(100,219,255,.30); background: rgba(100,219,255,.10); color: #dff7ff; }
    .schedule-history-status.running::before { background: var(--cyan); box-shadow: 0 0 6px var(--cyan); animation: pulse 1.4s ease-in-out infinite; }
    .schedule-history-time { color: var(--faint); font-size: clamp(9px, 1vw, 11px); font-family: ui-monospace, SFMono-Regular, Consolas, monospace; }
    .schedule-history-output { color: var(--soft); white-space: pre-wrap; word-break: break-word; margin-top: 6px; padding: 8px 10px; border-radius: 10px; background: rgba(3,7,16,.45); border: 1px solid rgba(255,255,255,.06); }
    .schedule-history-error { color: #ffd7df; white-space: pre-wrap; word-break: break-word; margin-top: 6px; padding: 8px 10px; border-radius: 10px; background: rgba(255,107,138,.10); border: 1px solid rgba(255,107,138,.18); }
    .task-empty { text-align: center; color: var(--faint); padding: 20px 10px; font-size: clamp(11px, 1.2vw, 13px); }

    @keyframes pulse { 0%, 100% { opacity: 1; } 50% { opacity: .4; } }

    @media (max-width: 1180px) {
      .schedule-landing { grid-template-columns: 1fr; }
      .schedule-panel .agent-main-body { grid-template-columns: 1fr; }
    }
    @media (max-width: 980px) {
      .schedule-panel .agent-main-body { padding: 12px; gap: 12px; }
      .schedule-panel .schedule-form, .schedule-panel .schedule-history-card { padding: 14px; }
      .schedule-item-actions { justify-content: flex-start; }
    }
    @media (max-width: 640px) {
      .schedule-panel .agent-hero { padding: 14px; }
      .schedule-panel .agent-hero::before { width: 42px; height: 42px; font-size: 18px; margin-bottom: 10px; }
      .schedule-landing { grid-template-columns: 1fr; gap: 12px; }
      .schedule-list { gap: 8px; }
      .schedule-item { padding: 12px 14px; border-radius: 14px; gap: 8px; }
      .schedule-item-actions { justify-content: flex-start; }
      .schedule-item-actions button { min-height: 28px; padding: 4px 10px; font-size: 11px; }
      .schedule-meta-cell { padding: 6px 8px; }
    }

    .skills-panel { overflow: hidden; }
    .skills-landing { height: 100%; min-height: 0; display: grid; grid-template-columns: minmax(260px, var(--unit-fr)) minmax(0, var(--phi-fr)); gap: 14px; }
    .skills-panel .agent-side { height: 100%; min-height: 0; padding: 12px; gap: 10px; align-content: stretch; grid-template-rows: auto auto minmax(0, 1fr); overflow: hidden; }
    .skills-panel .agent-hero { display: grid; grid-template-columns: 36px minmax(0, 1fr); column-gap: 11px; align-items: center; border-radius: 18px; padding: 14px; }
    .skills-panel .agent-hero::before { content: "🎯"; display: grid; place-items: center; width: 36px; height: 36px; border-radius: 13px; color: #06130d; background: linear-gradient(135deg, var(--green), var(--cyan)); font-size: 17px; grid-row: 1 / span 2; }
    .skills-panel .agent-hero h2 { font-size: clamp(17px, 1.7vw, 21px); margin: 0; letter-spacing: -.02em; }
    .skills-panel .agent-hero p { margin-top: 4px; color: var(--soft); line-height: 1.48; font-size: 12px; }
    .skills-panel .agent-side .control-card, .skills-panel .agent-side .note-card { border-radius: 18px; padding: 12px; }
    .skills-panel .agent-main-head { padding: 15px 18px; }
    .skills-panel .agent-main-body { min-height: 0; overflow: auto; padding: 14px; display: grid; grid-template-columns: minmax(420px, var(--phi-fr)) minmax(300px, var(--unit-fr)); gap: 12px; align-items: start; }
    .skills-panel .skills-form, .skills-panel .skills-preview-card { border: 1px solid var(--line); border-radius: 18px; padding: 14px; background: rgba(255,255,255,.045); box-shadow: inset 0 1px 0 rgba(255,255,255,.045); }
    .skills-panel .skills-list-card { min-height: 0; display: flex; flex-direction: column; padding: 12px; border-radius: 18px; background: rgba(255,255,255,.045); }
    .skills-panel .skills-form::before { content: var(--skills-editor-label, "SKILL EDITOR"); display: block; margin-bottom: 10px; color: var(--green); font-size: 10px; font-weight: 900; letter-spacing: .14em; }
    .skills-panel .skills-form .agent-form-grid { display: grid; grid-template-columns: minmax(0, var(--unit-fr)) minmax(0, var(--phi-fr)); gap: 10px 12px; }
    .skills-panel .skills-form .full { grid-column: 1 / -1; }
    .skills-panel .skills-form textarea { border: 1px solid var(--line); background: rgba(3,7,16,.62); border-radius: 14px; padding: 10px 12px; }
    .skills-panel #skillContent { min-height: clamp(220px, 36vh, 420px); font-family: ui-monospace, SFMono-Regular, Consolas, monospace; font-size: 12px; }
    .skills-panel .skills-form .agent-actions { justify-content: flex-start; flex-wrap: wrap; margin-top: 0; gap: 8px; }
    .skills-panel .skills-form .agent-actions button { min-width: 112px; min-height: 36px; border-radius: 12px; }
    .skills-panel .skill-learn-button { width: 100%; min-height: 38px; margin-top: 9px; border-radius: 12px; }
    .skills-panel .skills-list-card::before { content: var(--skills-library-label, "SKILL LIBRARY"); display: block; margin-bottom: 8px; color: var(--green); font-size: 10px; font-weight: 900; letter-spacing: .14em; }
    .skills-panel .skills-preview-card::before { content: var(--skills-preview-label, "PREVIEW"); display: block; margin-bottom: 8px; color: var(--green); font-size: 10px; font-weight: 900; letter-spacing: .14em; }
    .skills-list-head { display: flex; align-items: center; justify-content: space-between; gap: 8px; }
    .skills-list { flex: 1 1 auto; min-height: 0; overflow: auto; display: flex; flex-direction: column; gap: 7px; padding-right: 2px; }
    .skill-item { display: flex; align-items: flex-start; gap: 9px; padding: 10px 11px; border-radius: 14px; border: 1px solid var(--line); background: rgba(255,255,255,.03); cursor: pointer; transition: border-color .16s ease, background .16s ease, transform .16s ease, box-shadow .16s ease; animation: goldenCardIn .26s var(--ease-soft) both; }
    .skill-item:hover { transform: translateY(-1px); border-color: rgba(103,247,177,.35); background: rgba(103,247,177,.06); box-shadow: 0 10px 24px rgba(0,0,0,.16); }
    .skill-item.active { border-color: rgba(103,247,177,.55); background: rgba(103,247,177,.10); }
    .skill-item-icon { width: 30px; height: 30px; border-radius: 10px; display: flex; align-items: center; justify-content: center; background: rgba(103,247,177,.15); color: var(--green); font-size: 14px; flex-shrink: 0; }
    .skill-item-body { flex: 1; min-width: 0; }
    .skill-item-name { font-weight: 700; font-size: 13px; color: var(--ink); margin-bottom: 2px; }
    .skill-item-desc { font-size: 12px; color: var(--soft); line-height: 1.42; overflow: hidden; text-overflow: ellipsis; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; }
    .skill-item-tags { display: flex; flex-wrap: wrap; gap: 4px; margin-top: 5px; }
    .skill-tag { padding: 2px 7px; border-radius: 999px; border: 1px solid rgba(103,247,177,.25); background: rgba(103,247,177,.10); color: var(--green); font-size: 10px; font-weight: 700; }
    .skill-item-actions { display: flex; flex-direction: column; gap: 5px; flex-shrink: 0; opacity: .82; transition: opacity .16s; }
    .skill-item:hover .skill-item-actions { opacity: 1; }
    .skill-item-actions button { min-height: 26px; padding: 3px 8px; font-size: 11px; border-radius: 8px; }
    .skill-action-row { display: flex; gap: 5px; }
    .skill-action-row button { width: auto; flex: 1 1 0; }
    .skill-validate-button { width: 100%; }
    .skill-preview-content { max-height: min(58vh, 560px); overflow: auto; padding: 10px; border: 1px solid rgba(255,255,255,.08); border-radius: 14px; background: rgba(3,7,16,.36); font-size: 13px; line-height: 1.62; color: var(--soft); white-space: pre-wrap; }
    .skill-empty { display: block; text-align: center; padding: 28px 14px; color: var(--faint); font-size: 13px; }
    .skills-learn-overlay{position:fixed;inset:0;z-index:120;display:none;place-items:center;background:rgba(5,7,17,.82);backdrop-filter:blur(9px);}
    .skills-learn-overlay.active{display:grid;}
    .skills-learn-dialog{position:relative;width:min(760px,calc(100vw - 28px));max-height:min(88vh,780px);display:grid;grid-template-rows:auto minmax(0,1fr) auto;gap:14px;padding:18px;border:1px solid rgba(103,247,177,.24);border-radius:22px;background:linear-gradient(180deg,rgba(16,24,42,.96),rgba(7,11,22,.94));box-shadow:0 24px 80px rgba(0,0,0,.42),inset 0 1px 0 rgba(255,255,255,.06);}
    .skills-learn-head{display:flex;align-items:flex-start;justify-content:space-between;gap:14px;}
    .skills-learn-head strong{display:block;font-size:17px;color:var(--ink);}
    .skills-learn-head p{margin:5px 0 0;color:var(--soft);font-size:12px;line-height:1.5;}
    .skills-learn-close{width:34px;min-width:34px;height:34px;min-height:34px;padding:0;border-radius:12px;border:1px solid rgba(255,255,255,.12);background:rgba(255,255,255,.04);color:var(--soft);font-size:20px;line-height:1;display:grid;place-items:center;flex:0 0 auto;cursor:pointer;position:relative;z-index:2;}
    .skills-learn-close:hover{color:var(--ink);border-color:rgba(103,247,177,.35);background:rgba(103,247,177,.09);}
    .skills-learn-body{min-height:0;overflow:auto;display:grid;gap:12px;}
    .skills-learn-body textarea{min-height:220px;border:1px solid var(--line);border-radius:14px;background:rgba(3,7,16,.62);padding:10px 12px;resize:vertical;}
    .skills-learn-picker-row{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:8px;}
    .skills-learn-picker-row button{min-width:0;min-height:36px;border-radius:11px;white-space:normal;}
    .skills-learn-file-summary{min-height:34px;padding:9px 11px;border:1px dashed rgba(180,205,255,.18);border-radius:12px;background:rgba(3,7,16,.34);color:var(--faint);font-size:12px;line-height:1.35;word-break:break-word;}
    .skills-learn-hidden-input{position:absolute;width:1px;height:1px;opacity:0;pointer-events:none;}
    .skills-learn-actions{display:flex;justify-content:flex-end;gap:10px;}
    .skills-learn-actions button{width:auto;min-width:118px;min-height:38px;border-radius:12px;}
    @media (max-width: 1180px) {
      .skills-landing { grid-template-columns: 1fr; }
      .skills-panel .agent-side { height: auto; grid-template-rows: auto; grid-template-columns: minmax(0, 1fr); overflow: visible; }
      .skills-list { max-height: 320px; }
    }
    @media (max-width: 980px) {
      .skills-panel .agent-main-body { grid-template-columns: 1fr; }
    }
    @media (max-width: 640px) {
      .skills-panel .agent-main-body { padding: 12px; gap: 10px; }
      .skills-panel .agent-hero { grid-template-columns: 32px minmax(0, 1fr); padding: 12px; }
      .skills-panel .agent-hero::before { width: 32px; height: 32px; font-size: 15px; }
      .skills-panel .skills-form .agent-form-grid { grid-template-columns: 1fr; }
      .skills-panel .skills-form .agent-actions button { flex: 1 1 120px; }
      .skill-item { padding: 9px 10px; }
      .skill-item-actions { opacity: 1; }
    }

.notice-card{position:relative;overflow:hidden;border:1px solid rgba(103,247,177,.22);border-radius:20px;padding:16px 18px;background:linear-gradient(135deg,rgba(103,247,177,.10),rgba(100,219,255,.06) 48%,rgba(167,139,250,.08));box-shadow:0 12px 34px rgba(0,0,0,.22),inset 0 1px 0 rgba(255,255,255,.06);}
.notice-card::before{content:"⏱";position:absolute;right:14px;top:14px;font-size:20px;opacity:.35;}
.notice-head{display:flex;align-items:center;gap:10px;margin-bottom:10px;}
.notice-badge{display:inline-flex;align-items:center;gap:6px;padding:4px 10px;border-radius:999px;border:1px solid rgba(103,247,177,.28);background:rgba(103,247,177,.10);color:var(--green);font-size:11px;font-weight:800;letter-spacing:.06em;text-transform:uppercase;}
.notice-card .notice-title{font-size:14px;font-weight:700;color:var(--ink);margin-bottom:8px;}
.notice-card .notice-body{max-width:100%;overflow-x:auto;font-size:13px;line-height:1.65;color:var(--soft);}
.notice-card .notice-body table{display:block;max-width:100%;overflow-x:auto;border-collapse:collapse;}
.notice-time{margin-top:10px;font-size:11px;color:var(--faint);}
.schedule-rule-editor{display:grid;gap:12px;}
.schedule-rule-type{display:flex;flex-wrap:wrap;gap:8px;}
.schedule-rule-type button{width:auto;min-height:34px;padding:6px 14px;border-radius:999px;font-size:12px;}
.schedule-rule-type button.active{color:#06130d;border-color:rgba(103,247,177,.45);background:linear-gradient(135deg,var(--green),var(--cyan));}
.schedule-rule-fields{display:grid;gap:10px;}
.schedule-rule-row{display:grid;grid-template-columns:1fr 1fr;gap:10px;}
.schedule-rule-row.single{grid-template-columns:1fr;}
.schedule-rule-row input,.schedule-rule-row select{min-height:40px;border-radius:12px;}
.schedule-rule-chip{display:inline-flex;align-items:center;gap:6px;padding:5px 10px;border-radius:999px;border:1px solid rgba(100,219,255,.25);background:rgba(100,219,255,.08);color:#c8ecff;font-size:12px;}
.schedule-rule-chip button{width:auto;min-height:0;padding:0;border:0;background:transparent;color:var(--rose);font-size:14px;line-height:1;cursor:pointer;}
.schedule-test-overlay{position:fixed;inset:0;z-index:10030;display:none;place-items:center;padding:24px;background:rgba(2,6,14,.72);backdrop-filter:blur(12px);}
.schedule-test-overlay.active{display:grid;}
.schedule-test-card{width:min(520px,calc(100vw - 32px));display:grid;gap:13px;padding:20px;border:1px solid rgba(100,219,255,.24);border-radius:22px;background:linear-gradient(180deg,rgba(14,22,40,.96),rgba(6,10,20,.94));box-shadow:0 24px 80px rgba(0,0,0,.42),inset 0 1px 0 rgba(255,255,255,.06);}
.schedule-test-card strong{font-size:16px;color:var(--ink);}
.schedule-test-card p{margin:0;color:var(--soft);line-height:1.55;font-size:13px;word-break:break-word;}
.schedule-test-bar{height:8px;border-radius:999px;overflow:hidden;background:rgba(255,255,255,.08);}
.schedule-test-bar span{display:block;height:100%;width:36%;border-radius:999px;background:linear-gradient(90deg,var(--green),var(--cyan));box-shadow:0 0 14px rgba(103,247,177,.35);animation:scheduleTestSweep 1.25s ease-in-out infinite;}
.schedule-test-card small{color:var(--faint);font-size:11px;}
@keyframes scheduleTestSweep{0%{transform:translateX(-110%);}50%{transform:translateX(70%);}100%{transform:translateX(285%);}}

/* ===== Memory Panel ===== */
.memory-panel{overflow:hidden;}
.memory-landing{height:100%;min-height:0;display:grid;grid-template-columns:minmax(280px,360px) minmax(0,1fr);gap:16px;}
.memory-panel .agent-side{padding:14px;gap:12px;}
.memory-panel .agent-hero{border-radius:24px;padding:18px;}
.memory-panel .agent-hero h2{font-size:clamp(18px,2vw,24px);margin:0;letter-spacing:-.03em;}
.memory-panel .agent-hero p{margin-top:8px;color:var(--soft);line-height:1.65;font-size:clamp(11px,1.2vw,13px);}
.memory-nav-card{padding:12px;border-radius:22px;background:rgba(255,255,255,.045);box-shadow:inset 0 1px 0 rgba(255,255,255,.045);}
.memory-nav-tabs{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:8px;}
.memory-nav-tabs button{width:auto;min-height:40px;padding:8px 14px;border-radius:14px;font-size:clamp(12px,1.3vw,14px);font-weight:700;color:var(--soft);background:rgba(255,255,255,.06);border:1px solid rgba(255,255,255,.10);transition:all .15s ease;}
.memory-nav-tabs button:hover{background:rgba(255,255,255,.10);color:var(--ink);}
.memory-nav-tabs button.active{color:#06130d;border-color:rgba(103,247,177,.45);background:linear-gradient(135deg,var(--green),var(--cyan));box-shadow:0 4px 16px rgba(103,247,177,.20);}
.memory-panel .agent-main-head{padding:16px 18px;}
.memory-panel .agent-main-body{height:100%;min-height:0;overflow:hidden;padding:16px;display:grid;grid-template-rows:minmax(0,1fr);align-items:stretch;}
.memory-view{display:none;min-height:0;overflow:hidden;}
.memory-view.active{display:flex;flex-direction:column;height:100%;min-height:0;overflow:hidden;}
#memoryCoreView{gap:12px;}
.memory-editors{display:grid;grid-template-columns:1fr 1fr;grid-template-rows:1fr 1fr;gap:12px;flex:1;min-height:0;}
.memory-editor-card{overflow:hidden;display:flex;flex-direction:column;border:1px solid rgba(255,255,255,.06);border-radius:16px;padding:16px;background:rgba(255,255,255,.03);}
.memory-editor-header{display:flex;align-items:center;justify-content:space-between;gap:8px;margin-bottom:10px;flex-shrink:0;}
.memory-editor-header strong{font-size:13px;color:var(--ink);}
.memory-hint{color:var(--faint);font-size:11px;}
#memoryReload{grid-column:1 / -1;}
#memoryOrganizeHint{line-height:1.45;margin:4px 0 0;}
.memory-snapshot-panel{border:1px solid rgba(255,255,255,.08);background:rgba(3,7,16,.28);padding:12px;display:grid;gap:10px;}
.memory-snapshot-panel .memory-editor-header{margin:0;}
.memory-snapshot-actions{display:grid;grid-template-columns:minmax(0,1fr) auto;gap:10px;align-items:center;}
.memory-snapshot-actions select{min-width:0;}
.memory-editor-card textarea{flex:1;width:100%;min-height:0;border:1px solid rgba(255,255,255,.08);background:rgba(3,7,16,.45);border-radius:10px;padding:10px;font-family:ui-monospace,SFMono-Regular,Consolas,monospace;font-size:12px;line-height:1.6;color:var(--ink);resize:none;}
.memory-editor-card textarea:focus{outline:none;border-color:rgba(103,247,177,.3);}
.memory-actions{display:flex;gap:10px;flex-shrink:0;padding-top:4px;}
.memory-actions button{min-width:120px;height:36px;}
.memory-ltm-layout{display:grid;grid-template-columns:1fr 1fr;gap:16px;height:100%;min-height:0;overflow:hidden;align-items:stretch;}
.memory-ltm-list{display:flex;flex-direction:column;gap:12px;min-height:0;overflow:hidden;}
.memory-ltm-search{display:flex;gap:8px;align-items:center;}
.memory-ltm-search input[type="date"]{flex:1;min-height:36px;border:1px solid var(--line);background:rgba(3,7,16,.62);border-radius:10px;padding:6px 10px;color:var(--ink);font-size:12px;}
.memory-ltm-search-sep{color:var(--faint);font-size:12px;}
.memory-ltm-search button{width:auto;min-height:36px;padding:6px 14px;font-size:12px;}
.memory-list{display:flex;flex-direction:column;gap:8px;flex:1 1 0;min-height:0;overflow:auto;overscroll-behavior:contain;}
.memory-item{position:relative;padding:14px 16px;border:1px solid rgba(255,255,255,.10);border-radius:16px;background:linear-gradient(135deg,rgba(255,255,255,.055),rgba(255,255,255,.025));cursor:pointer;transition:all .15s ease;}
.memory-item:hover{border-color:rgba(103,247,177,.32);background:rgba(103,247,177,.06);transform:translateY(-1px);}
.memory-item.active{border-color:rgba(103,247,177,.45);background:rgba(103,247,177,.10);box-shadow:0 4px 16px rgba(103,247,177,.15);}
.memory-item::before{content:"";position:absolute;inset:0 auto 0 0;width:3px;background:linear-gradient(var(--green),var(--cyan));opacity:.7;border-radius:0 3px 3px 0;}
.memory-item-header{display:flex;align-items:baseline;justify-content:space-between;gap:8px;margin-bottom:8px;}
.memory-item-id{font-size:15px;font-weight:700;color:var(--ink);letter-spacing:-.01em;}
.memory-item-time{font-size:11px;color:var(--faint);font-family:ui-monospace,SFMono-Regular,Consolas,monospace;}
.memory-item-content{font-size:11px;color:var(--soft);line-height:1.6;display:-webkit-box;-webkit-line-clamp:3;-webkit-box-orient:vertical;overflow:hidden;}
.memory-ltm-preview{height:100%;min-height:0;overflow:hidden;border:1px solid var(--line);border-radius:20px;padding:16px;background:rgba(255,255,255,.045);box-shadow:inset 0 1px 0 rgba(255,255,255,.045);display:flex;flex-direction:column;gap:12px;}
.memory-preview-header{display:flex;align-items:center;justify-content:space-between;gap:10px;}
.memory-preview-header strong{font-size:14px;color:var(--ink);}
.memory-preview-content{flex:1 1 0;min-height:0;max-height:100%;overflow:auto;overscroll-behavior:contain;padding:12px;border:1px solid rgba(255,255,255,.08);border-radius:14px;background:rgba(3,7,16,.38);font-size:12px;line-height:1.7;color:var(--soft);white-space:pre-wrap;word-break:break-word;}
.memory-vector-layout{display:grid;grid-template-columns:minmax(260px,38.2%) minmax(0,61.8%);gap:16px;height:100%;min-height:0;}
.memory-vector-panel{min-height:0;border:1px solid rgba(255,255,255,.075);border-radius:18px;background:rgba(255,255,255,.035);box-shadow:inset 0 1px 0 rgba(255,255,255,.045);display:flex;flex-direction:column;overflow:hidden;}
.memory-vector-panel-head{display:flex;align-items:center;justify-content:space-between;gap:10px;padding:14px 14px 10px;border-bottom:1px solid rgba(255,255,255,.06);}
.memory-vector-panel-head strong{font-size:13px;color:var(--ink);letter-spacing:.01em;}
.memory-vector-meta{display:flex;flex-wrap:wrap;gap:6px;color:var(--faint);font-size:11px;}
.memory-vector-meta span,.memory-vector-chip{display:inline-flex;align-items:center;gap:5px;padding:4px 8px;border-radius:999px;border:1px solid rgba(255,255,255,.08);background:rgba(3,7,16,.34);}
.memory-vector-search{display:flex;gap:8px;padding:12px 14px;border-bottom:1px solid rgba(255,255,255,.06);}
.memory-vector-search input{flex:1;min-width:0;min-height:38px;border:1px solid var(--line);border-radius:12px;background:rgba(3,7,16,.58);color:var(--ink);padding:8px 11px;font-size:12px;}
.memory-vector-search input:focus{outline:none;border-color:rgba(103,247,177,.32);box-shadow:0 0 0 3px rgba(103,247,177,.08);}
.memory-vector-search button{width:auto;min-height:38px;padding:8px 14px;font-size:12px;}
.memory-vector-results{flex:1;min-height:0;overflow:auto;padding:12px 14px;display:flex;flex-direction:column;gap:9px;}
.memory-vector-result{position:relative;border:1px solid rgba(255,255,255,.085);border-radius:15px;background:linear-gradient(135deg,rgba(255,255,255,.052),rgba(255,255,255,.024));padding:12px 13px;cursor:pointer;transition:transform .16s ease,border-color .16s ease,background .16s ease,box-shadow .16s ease;}
.memory-vector-result:hover{transform:translateY(-1px);border-color:rgba(100,219,255,.30);background:rgba(100,219,255,.065);}
.memory-vector-result.active{border-color:rgba(103,247,177,.48);background:rgba(103,247,177,.085);box-shadow:0 8px 24px rgba(103,247,177,.10);}
.memory-vector-result-title{display:flex;align-items:center;justify-content:space-between;gap:8px;margin-bottom:7px;}
.memory-vector-result-title strong{font-size:13px;color:var(--ink);min-width:0;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;}
.memory-vector-score{font-family:ui-monospace,SFMono-Regular,Consolas,monospace;font-size:11px;color:var(--green);}
.memory-vector-source{font-size:11px;color:var(--faint);font-family:ui-monospace,SFMono-Regular,Consolas,monospace;margin-bottom:7px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;}
.memory-vector-preview{font-size:11px;line-height:1.65;color:var(--soft);display:-webkit-box;-webkit-line-clamp:3;-webkit-box-orient:vertical;overflow:hidden;}
.memory-vector-terms{display:flex;flex-wrap:wrap;gap:5px;margin-top:9px;}
.memory-vector-terms span{font-size:10px;color:#c8ecff;border:1px solid rgba(100,219,255,.20);border-radius:999px;padding:2px 6px;background:rgba(100,219,255,.07);}
.memory-vector-map{position:relative;flex:1;min-height:0;background:radial-gradient(circle at 35% 25%,rgba(103,247,177,.08),transparent 36%),radial-gradient(circle at 70% 72%,rgba(100,219,255,.08),transparent 38%),rgba(3,7,16,.36);}
.memory-vector-canvas{display:block;width:100%;height:100%;min-height:360px;}
.memory-vector-tooltip{position:absolute;left:12px;bottom:12px;max-width:min(420px,calc(100% - 24px));padding:10px 12px;border:1px solid rgba(255,255,255,.10);border-radius:14px;background:rgba(5,9,19,.84);box-shadow:0 14px 40px rgba(0,0,0,.25);backdrop-filter:blur(10px);font-size:11px;color:var(--soft);pointer-events:none;opacity:0;transform:translateY(6px);transition:opacity .16s ease,transform .16s ease;}
.memory-vector-tooltip.active{opacity:1;transform:translateY(0);}
.memory-vector-tooltip strong{display:block;margin-bottom:4px;color:var(--ink);font-size:12px;}
.memory-vector-empty{padding:16px;border:1px dashed rgba(255,255,255,.12);border-radius:14px;color:var(--faint);font-size:12px;text-align:center;background:rgba(255,255,255,.025);}
    @media(max-width:1180px){.memory-landing{grid-template-columns:1fr;}.memory-ltm-layout{grid-template-columns:1fr;grid-template-rows:minmax(0,1fr) minmax(280px,1fr);}.memory-editors{grid-template-columns:1fr;}.memory-vector-layout{grid-template-columns:1fr;}.memory-vector-canvas{min-height:420px;}}
    @media(max-width:980px){.memory-panel .agent-main-body{padding:12px;}.memory-nav-tabs button{min-height:36px;padding:6px 12px;}.memory-ltm-preview{max-height:min(62vh,620px);}.memory-preview-content{max-height:min(52vh,540px);}}

    /* ===== Organize Progress Bar ===== */
    .organize-overlay{position:fixed;inset:0;z-index:100;display:none;place-items:center;background:rgba(5,7,17,.85);backdrop-filter:blur(8px);}
    .organize-overlay.active{display:grid;}
    .organize-card{width:min(480px,90vw);padding:28px;border:1px solid rgba(103,247,177,.25);border-radius:26px;background:linear-gradient(135deg,rgba(16,24,42,.95),rgba(7,11,22,.92));box-shadow:0 24px 80px rgba(0,0,0,.42),inset 0 1px 0 rgba(255,255,255,.06);}
    .organize-card h3{margin:0 0 8px;font-size:18px;color:var(--green);}
    .organize-card p{margin:0 0 18px;color:var(--soft);font-size:13px;}
    .organize-progress{height:8px;border-radius:999px;background:rgba(255,255,255,.08);overflow:hidden;}
    .organize-progress-bar{height:100%;width:0%;border-radius:999px;background:linear-gradient(90deg,var(--green),var(--cyan));transition:width .4s ease;box-shadow:0 0 12px rgba(103,247,177,.35);}
    .organize-percent{margin-top:10px;text-align:center;font-size:24px;font-weight:800;color:var(--green);font-family:ui-monospace,SFMono-Regular,Consolas,monospace;}
    .organize-stage{margin-top:6px;text-align:center;font-size:12px;color:var(--faint);}
    .organize-error{margin-top:6px;text-align:center;font-size:11px;color:#e74c3c;font-weight:500;max-width:420px;word-break:break-word;line-height:1.4;}
    .muted-status{color:var(--faint);font-size:12px;}
    .multimodal-card{display:block;align-items:stretch;padding:18px;background:linear-gradient(180deg,rgba(255,255,255,.052),rgba(255,255,255,.026));}
    .multimodal-card > .settings-card-copy{max-width:820px;}
    .multimodal-card > .settings-card-copy strong{font-size:16px;}
    .multimodal-toolbar{display:flex;align-items:center;justify-content:space-between;gap:12px;margin-top:16px;padding-top:14px;border-top:1px solid rgba(255,255,255,.08);flex-wrap:wrap;min-width:0;}
    .multimodal-toolbar .button-row{grid-template-columns:repeat(2,minmax(112px,1fr));min-width:min(100%,250px);}
    .multimodal-grid{display:grid;grid-template-columns:minmax(0,1fr);gap:14px;margin-top:14px;align-items:start;}
    .multimodal-profile{border:1px solid rgba(255,255,255,.10);border-radius:16px;background:rgba(3,7,16,.36);padding:14px;min-width:0;overflow:hidden;box-shadow:inset 0 1px 0 rgba(255,255,255,.045);}
    .multimodal-profile h3{margin:0 0 12px;color:var(--ink);font-size:14px;line-height:1.25;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;}
    .multimodal-section{border-top:1px solid rgba(255,255,255,.08);padding-top:12px;margin-top:12px;}
    .multimodal-section:first-child{border-top:0;margin-top:0;padding-top:0;}
    .multimodal-section-title{display:flex;align-items:center;justify-content:space-between;gap:10px;margin-bottom:10px;color:var(--green);font-size:11px;font-weight:850;text-transform:uppercase;letter-spacing:.06em;}
    .multimodal-section-title::after{content:"";height:1px;flex:1;background:linear-gradient(90deg,rgba(103,247,177,.26),transparent);}
    .multimodal-section-title button{width:auto;min-width:68px;min-height:28px;padding:0 10px;border-radius:9px;font-size:11px;letter-spacing:0;text-transform:none;}
    .multimodal-form-grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr));gap:9px;}
    .multimodal-form .field{margin:0;min-width:0;}
    .multimodal-form .multimodal-wide-field{grid-column:1 / -1;}
    .multimodal-form input,.multimodal-form select{min-height:37px;border-radius:12px;min-width:0;}
    .multimodal-form input[type="checkbox"]{width:auto;min-height:0;accent-color:#67f7b1;}
    .multimodal-check{display:flex;align-items:center;gap:8px;color:var(--soft);font-size:12px;min-height:34px;}
    .image-profile-list{display:grid;gap:10px;}
    .image-profile-card{min-width:0;display:grid;gap:10px;padding:12px;border:1px solid rgba(255,255,255,.08);border-radius:14px;background:rgba(255,255,255,.025);}
    .image-profile-head{display:flex;align-items:center;justify-content:space-between;gap:10px;min-width:0;}
    .image-profile-head strong{min-width:0;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;font-size:13px;color:var(--ink);}
    .image-profile-remove{width:auto;min-width:68px;min-height:28px;padding:0 10px;border-radius:9px;font-size:11px;}
    .multimodal-note{margin:8px 0 0;color:var(--faint);font-size:12px;line-height:1.5;}
    .message-footer{display:none;}
    .meta .audio-button{width:23px!important;min-width:23px;max-width:23px;flex:0 0 23px;min-height:22px;height:22px;padding:0;display:inline-grid;place-items:center;border:0;border-radius:0;color:var(--cyan);background:transparent;box-shadow:none;opacity:.72;transform:none;filter:none;transition:color .16s var(--ease-smooth),opacity .16s var(--ease-smooth),text-shadow .16s var(--ease-smooth),transform .16s var(--ease-smooth);}
    .meta .audio-button:hover:not(:disabled){color:var(--green);background:transparent;border-color:transparent;box-shadow:none;opacity:1;filter:none;transform:translateY(-1px);text-shadow:0 0 16px rgba(103,247,177,.34);}
    .meta .audio-button.active{color:var(--green);background:transparent;border-color:transparent;box-shadow:none;opacity:1;text-shadow:0 0 18px rgba(103,247,177,.42);}
    .meta .audio-button.loading{cursor:wait;color:var(--green);background:transparent;border-color:transparent;box-shadow:none;opacity:1;}
    .speaker-icon{width:18px;height:18px;display:block;stroke:currentColor;fill:none;stroke-width:2;stroke-linecap:round;stroke-linejoin:round;}
    .audio-button.loading .speaker-icon{animation:speakerBounce .48s ease-in-out infinite;}
    .audio-button.ready-pulse .speaker-icon{animation:speakerReady .42s ease;}
    .tts-error-overlay{position:fixed;inset:0;z-index:10020;display:none;align-items:center;justify-content:center;padding:24px;background:rgba(2,6,14,.66);backdrop-filter:blur(12px);animation:fadeIn .18s ease both;}
    .tts-error-overlay.active{display:flex;}
    .tts-error-card{width:min(560px,92vw);max-height:min(420px,82vh);display:grid;gap:10px;padding:18px;border:1px solid rgba(255,107,107,.28);border-radius:18px;background:linear-gradient(180deg,rgba(33,10,16,.96),rgba(10,15,25,.96));box-shadow:0 24px 72px rgba(0,0,0,.45);cursor:default;overflow:auto;}
    .tts-error-card strong{color:#ffd9df;font-size:15px;line-height:1.25;}
    .tts-error-card p{margin:0;color:var(--soft);font-size:12px;line-height:1.55;white-space:pre-wrap;overflow-wrap:anywhere;}
    @keyframes speakerBounce{0%,100%{transform:translateY(0) scale(1);}50%{transform:translateY(-2px) scale(1.08);}}
    @keyframes speakerReady{0%{transform:scale(1);}45%{transform:scale(1.18);}100%{transform:scale(1);}}
    .lab-landing{height:100%;min-height:0;display:grid;grid-template-columns:minmax(240px,var(--unit-fr)) minmax(0,var(--phi-fr));gap:16px;}
    .lab-side{padding:14px;gap:12px;}
    .lab-panel .agent-main-head{padding:16px 19px;}
    .lab-body{min-height:0;overflow:auto;padding:16px;display:grid;grid-template-columns:minmax(0,var(--phi-fr)) minmax(252px,var(--unit-fr));grid-template-areas:"image speech" "image transcribe";gap:14px;align-content:start;align-items:stretch;}
    .lab-card{display:flex;flex-direction:column;gap:12px;min-height:0;overflow:hidden;padding:16px;border-radius:18px;background:linear-gradient(180deg,rgba(255,255,255,.052),rgba(255,255,255,.025));}
    .lab-image-card{grid-area:image;min-height:560px;}
    .lab-tts-card{grid-area:speech;}
    .lab-stt-card{grid-area:transcribe;}
    .lab-card-head{display:flex;align-items:center;justify-content:space-between;gap:10px;min-height:30px;}
    .lab-card-head strong{min-width:0;color:var(--ink);font-size:15px;line-height:1.25;overflow-wrap:anywhere;}
    .lab-card-head span{max-width:54%;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;padding:5px 8px;border:1px solid rgba(103,247,177,.20);border-radius:999px;color:var(--green);background:rgba(103,247,177,.065);font-size:10px;font-family:ui-monospace,SFMono-Regular,Consolas,monospace;}
    .lab-card textarea{min-height:108px;resize:vertical;border:1px solid rgba(255,255,255,.10);border-radius:14px;background:rgba(3,7,16,.35);padding:10px 11px;}
    .lab-image-card textarea{min-height:172px;}
    .lab-row{display:flex;align-items:center;gap:9px;flex-wrap:wrap;}
    .lab-row > input,.lab-row > select{flex:1 1 150px;min-width:0;min-height:38px;border-radius:12px;}
    .lab-row > button{width:auto;min-width:108px;min-height:38px;border-radius:12px;white-space:normal;}
    .lab-result{min-height:94px;border:1px dashed rgba(255,255,255,.14);border-radius:14px;padding:12px;background:rgba(0,0,0,.18);overflow:auto;color:var(--soft);font-size:12px;line-height:1.55;}
    .lab-image-card .lab-result{flex:1;min-height:350px;display:grid;align-content:start;}
    .lab-result:empty::before{content:var(--lab-empty-text,"No result yet.");color:var(--faint);}
    .lab-result-meta{margin:0 0 10px;padding:7px 9px;border:1px solid rgba(103,247,177,.18);border-radius:10px;background:rgba(103,247,177,.055);color:var(--green);font-size:11px;line-height:1.35;overflow-wrap:anywhere;}
    .lab-result img{max-width:100%;border-radius:12px;display:block;box-shadow:0 18px 46px rgba(0,0,0,.26);}
    .lab-transcript{margin:0;min-height:168px;white-space:pre-wrap;border:1px dashed rgba(255,255,255,.14);border-radius:14px;padding:12px;background:rgba(0,0,0,.18);color:var(--soft);font-size:12px;line-height:1.55;overflow:auto;}
    @media (max-width: 1280px){.lab-body{grid-template-columns:1fr;grid-template-areas:"image" "speech" "transcribe";}.lab-image-card{min-height:auto;}.lab-image-card .lab-result{min-height:300px;}}
    @media (max-width: 1180px){.lab-landing{grid-template-columns:1fr;}.lab-side{grid-template-columns:minmax(0,var(--phi-fr)) minmax(220px,var(--unit-fr));align-items:stretch;}.lab-side .note-card{grid-column:1 / -1;}.multimodal-grid{grid-template-columns:1fr;}}
    @media (max-width: 760px){.multimodal-form-grid{grid-template-columns:1fr;}.multimodal-toolbar{align-items:stretch}.multimodal-toolbar .button-row{width:100%;}.multimodal-grid{grid-template-columns:1fr;}.lab-body{padding:12px;gap:10px;}.lab-side{grid-template-columns:1fr;}.lab-row > button{width:100%;}.lab-card-head{align-items:flex-start;flex-direction:column;}.lab-card-head span{max-width:100%;}}
</style>
</head>
<body>
  <div class="app-scale-stage">
    <div class="home-shell">
    <section id="homePage" class="tab-panel home-page active" role="region" aria-label="Matdance Star Map">
      <div class="home-landing">
        <canvas id="starMap" class="star-map" aria-label="Matdance 3D star map"></canvas>
        <div class="home-vignette"></div>
        <div id="warpOverlay" class="warp-overlay" aria-hidden="true"><div class="warp-core"></div></div>
        <div class="home-ui">
          <header class="home-hero">
            <div>
              <span class="home-eyebrow">Matrix Dance Star Map</span>
              <h1 id="matdanceTitle" class="glitch-title" data-text="MATDANCE" role="button" tabindex="0" aria-label="Matdance">MATDANCE</h1>
              <p class="home-subtitle">选择着陆地点...</p>
            </div>
            <aside id="planetHud" class="planet-hud">
              <span class="hud-kicker">Awaiting target</span>
              <strong>选择一个星球</strong>
              <p>拖动旋转星图，滚轮前进或后退，右键拖动横移。悬停星球查看目的地，点击后跃迁进入页面。</p>
            </aside>
          </header>
          <div></div>
          <footer class="home-help">
            <div class="control-readout">
              <span>左键拖动：旋转摄像机</span>
              <span>滚轮：前进 / 后退</span>
              <span>右键拖动：横移</span>
              <span>静止 3 秒：回归原点</span>
            </div>
            <div class="planet-dock" aria-label="Fallback landing controls">
              <button class="planet-chip" type="button" data-tab="chat">Chat</button>
              <button class="planet-chip" type="button" data-tab="agent">Agent</button>
              <button class="planet-chip" type="button" data-tab="schedule">Schedule</button>
              <button class="planet-chip" type="button" data-tab="skills">Skills</button>
              <button class="planet-chip" type="button" data-tab="lab">Lab</button>
              <button class="planet-chip" type="button" data-tab="settings">Settings</button>
              <button class="planet-chip" type="button" disabled>Workspace</button>
              <button class="planet-chip" type="button" data-tab="memory">Memory</button>
            </div>
          </footer>
        </div>
      </div>
    </section>

    <section id="chatTab" class="tab-panel chat-panel" role="tabpanel" aria-label="Chat">
      <div class="app">
    <aside class="sidebar">
      <section class="brand-card">
        <div class="brand-top">
          <div class="logo"><img src="/assets/brand/matdance-icon.png" alt="" /></div>
          <div>
            <h1>Matdance</h1>
            <p>Local C# Agent Console</p>
          </div>
        </div>
        <div class="brand-sub">
          <span class="chip">Streaming</span>
          <span class="chip">Tools</span>
          <span class="chip">Memory</span>
        </div>
      </section>

      <section class="control-card">
        <div class="field">
          <label for="agentSelect"><span>Agent</span><span>profile</span></label>
          <select id="agentSelect"></select>
        </div>
        <div class="field">
          <label for="sessionSelect"><span>Session</span><span>timeline</span></label>
          <select id="sessionSelect"></select>
        </div>
        <div class="button-row">
          <button id="newSession" class="primary">New Session</button>
          <button id="refresh" class="ghost">Refresh</button>
        </div>
      </section>

      <section class="metric-card">
        <div class="metric-grid">
          <div class="metric-line"><span>Model</span><strong id="model">-</strong></div>
          <div class="metric-line"><span>Context</span><strong id="ctxText">0%</strong></div>
          <div class="ctx-shell"><div class="ctx-fill" id="ctxFill"></div></div>
          <div class="metric-line"><span>Messages</span><strong id="msgCount">0</strong></div>
          <div class="metric-line"><span>Tool Calls</span><strong id="toolCount">0</strong></div>
        </div>
      </section>

      <section id="taskCard" class="task-card"></section>

      <section class="note-card">
        <strong>Interaction</strong><br />
        Enter 发送，Shift + Enter 换行。工具调用会嵌在 AI 卡片内，流式响应不会打断界面。Web 模式默认禁用交互式 bash，避免服务端阻塞确认。
      </section>
    </aside>

    <main class="stage">
      <header class="topbar">
        <div class="title-block">
          <div class="window-dots"><button id="winClose" type="button" title="Back to Home"></button><button id="winMin" type="button" title="Back to Home"></button><button id="winMax" type="button" title="Fullscreen"></button></div>
          <h2 id="title">Ready</h2>
          <small id="subtitle">Choose an agent and start chatting.</small>
        </div>
        <div style="display:flex;align-items:center;gap:10px;"><button id="browserBtn" class="tab-button" type="button" title="Browser">🌐 Browser</button><div class="phase-pill"><span class="phase-dot"></span><span id="phase">idle</span></div></div>
      </header>

      <section id="chat" class="chat"></section>
      <button id="chatJumpBottom" class="chat-jump-bottom" type="button" hidden>Bottom</button>

      <form id="composer" class="composer">
        <div class="composer-box">
          <div id="commandMenu" class="command-menu" hidden></div>
          <textarea id="input" placeholder="输入消息，让 agent 开始处理..." autocomplete="off"></textarea>
          <button id="voiceHold" class="voice-hold" type="button" hidden><span class="voice-hold-icon" aria-hidden="true"><svg viewBox="0 0 24 24"><path d="M12 2a3 3 0 0 0-3 3v7a3 3 0 0 0 6 0V5a3 3 0 0 0-3-3Z"></path><path d="M19 10v2a7 7 0 0 1-14 0v-2"></path><path d="M12 19v3"></path></svg></span><span class="voice-hold-copy"><span class="voice-hold-title">长按录制</span><small>松开发送，上滑取消</small></span></button>
          <div class="composer-actions">
            <div class="file-zone">
              <button id="attachButton" class="ghost attach-btn" type="button" title="Attach files" aria-label="Attach files">+</button>
              <input id="chatAttachmentInput" class="chat-attachment-input" type="file" multiple />
              <div id="attachmentStrip" class="attachment-strip"><span class="attachment-empty" id="hint">本地 Web UI 已连接。</span></div>
            </div>
            <div class="composer-buttons">
              <button id="micButton" class="ghost mic" type="button" hidden title="Voice input" aria-label="Voice input"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 2a3 3 0 0 0-3 3v7a3 3 0 0 0 6 0V5a3 3 0 0 0-3-3Z"></path><path d="M19 10v2a7 7 0 0 1-14 0v-2"></path><path d="M12 19v3"></path></svg></button>
              <button id="stopResponse" class="danger stop" type="button" disabled>Stop</button>
              <button id="send" class="primary send" type="submit">Send</button>
            </div>
          </div>
        </div>
      </form>
    </main>
      </div>
    </section>

    <div id="voiceRecordOverlay" class="voice-record-overlay" aria-hidden="true">
      <div class="voice-record-card">
        <div id="voiceRecordTime" class="voice-record-time">0.0s</div>
        <div class="voice-wave" aria-hidden="true"><span></span><span></span><span></span><span></span><span></span></div>
        <div id="voiceRecordHint" class="voice-record-hint">松开发送，上滑取消</div>
      </div>
    </div>

    <!-- Browser Overlay -->
    <div id="browserOverlay" class="browser-overlay">
      <div class="browser-window" id="browserWindow">
        <div class="browser-titlebar">
          <div class="window-dots">
            <button id="browserWinClose" type="button" title="Close"></button>
            <button id="browserWinMin" type="button" title="Minimize"></button>
            <button id="browserWinMax" type="button" title="Fullscreen"></button>
          </div>
          <div class="browser-title"><span id="browserTitle">Live Browser</span></div>
        </div>
        <div class="browser-content">
          <img id="browserFrame" class="browser-frame" alt="browser screencast" />
          <div id="browserPlaceholder" class="browser-placeholder">
            <div>🔌 WebSocket 连接中...</div>
            <div style="font-size:12px;opacity:.6;margin-top:8px;">请在 agent 对话中使用 browser_navigate 启动浏览器</div>
          </div>
        </div>
      </div>
    </div>

    <section id="agentTab" class="tab-panel agent-panel" role="tabpanel" aria-label="Agent">
      <div class="agent-landing">
        <aside class="agent-side">
          <section class="agent-hero">
            <h2>Agent Center</h2>
            <p>Configure the selected local agent without leaving the Web UI. API keys stay hidden; leaving the key field blank keeps the existing value.</p>
          </section>

          <section class="control-card">
            <div class="field">
              <label for="agentConfigSelect"><span>Agent</span><span>config target</span></label>
              <select id="agentConfigSelect"></select>
            </div>
            <div class="button-row">
              <button id="agentReload" class="ghost" type="button">Reload</button>
              <button id="agentSaveTop" class="primary" type="button">Save</button>
            </div>
            <div class="button-row agent-manage-actions">
              <button id="agentCreate" class="ghost" type="button">New Agent</button>
              <button id="agentDelete" class="danger" type="button">Delete</button>
            </div>
          <section class="agent-avatar-card">
            <div class="agent-avatar-top">
              <div id="agentAvatarPreview" class="agent-avatar-preview">A</div>
              <div class="agent-avatar-copy">
                <strong id="agentAvatarName">Agent Avatar</strong>
                <span id="agentAvatarHint" hidden></span>
              </div>
            </div>
            <input id="agentIconInput" type="file" accept="image/jpeg,image/png,image/svg+xml,image/gif,image/webp,.svg,.ico" />
            <button id="agentIconUpload" class="ghost" type="button">Upload Avatar</button>
          </section>
          </section>

          <section class="agent-status-grid">
            <div class="agent-status-card"><span>Sessions</span><strong id="agentSessionCount">-</strong></div>
            <div class="agent-status-card"><span>API Key</span><strong id="agentKeyState">-</strong></div>
            <div class="agent-status-card"><span>Hot Memory</span><strong id="agentHotMemoryState">-</strong></div>
            <div class="agent-status-card"><span>Core Memory</span><strong id="agentCoreMemoryState">-</strong></div>
          </section>

          <section class="agent-tabs-reserve">
            <div class="reserve-title">Reserved Tabs</div>
            <div class="reserve-grid">
              <span class="reserve-chip">Workspace</span>
              <span class="reserve-chip">Memory</span>
              <span class="reserve-chip">Tools</span>
              <span class="reserve-chip">Runs</span>
              <span class="reserve-chip">Settings</span>
            </div>
          </section>
        </aside>

        <main class="agent-main">
          <header class="agent-main-head">
            <div class="title-block">
              <div class="window-dots"><button id="agentWinClose" type="button" title="Back to Home"></button><button id="agentWinMin" type="button" title="Back to Home"></button><button id="agentWinMax" type="button" title="Fullscreen"></button></div>
              <h2>Agent Configuration</h2>
              <p id="agentConfigState">Select an agent to load configuration.</p>
            </div>
            <span class="phase-pill"><span class="phase-dot"></span><span>local config</span></span>
          </header>

          <section class="agent-main-body">
            <form id="agentForm" class="agent-form">
              <div class="agent-form-grid">
                <div class="agent-field">
                  <label for="configName"><span>Name</span><span>folder id</span></label>
                  <input id="configName" type="text" readonly />
                  <small>Agent identity follows the local folder name.</small>
                </div>
                <div class="agent-field">
                  <label for="configApiType"><span>API Type</span><span>provider</span></label>
                  <select id="configApiType"></select>
                  <small id="apiTypeHelp">OpenAI-compatible uses chat completions. Anthropic means a Messages-compatible endpoint and supports tools.</small>
                </div>
                <div class="agent-field full">
                  <label for="configBaseUrl"><span>Base URL</span><span>endpoint</span></label>
                  <input id="configBaseUrl" type="url" placeholder="https://..." />
                </div>
                <div class="agent-field">
                  <label for="configModelId"><span>Model</span><span>model_id</span></label>
                  <div id="configModelCombo" class="model-combo">
                    <input id="configModelId" type="text" placeholder="gpt-5.5" autocomplete="off" role="combobox" aria-expanded="false" aria-controls="configModelOptions" />
                    <button id="configModelToggle" class="model-combo-toggle" type="button" aria-label="Show model list"><span class="model-combo-arrow" aria-hidden="true"></span></button>
                    <div id="configModelOptions" class="model-combo-menu" role="listbox" hidden></div>
                  </div>
                </div>
                <div class="agent-field">
                  <label for="configApiKey"><span>API Key</span><span>hidden on load</span></label>
                  <input id="configApiKey" type="password" placeholder="Leave blank to keep current key" autocomplete="new-password" />
                  <a id="configApiKeyLink" class="provider-key-link" href="#" target="_blank" rel="noopener noreferrer" hidden>Get API Key here</a>
                </div>
                <div class="agent-field">
                  <label for="configContextWindow"><span>Context</span><span>tokens</span></label>
                  <input id="configContextWindow" type="number" min="1" step="1" />
                </div>
                <div class="agent-field">
                  <label for="configMaxOutputToken"><span>Max Output</span><span>tokens</span></label>
                  <input id="configMaxOutputToken" type="number" min="1" step="1" />
                </div>
                <div class="agent-field">
                  <label for="configMaxConcurrency"><span>Concurrency</span><span>agent budget</span></label>
                  <input id="configMaxConcurrency" type="number" min="1" max="16" step="1" />
                </div>
                <div class="agent-field">
                  <label for="configTemperature"><span>Temperature</span><span>0 - 2</span></label>
                  <input id="configTemperature" type="number" min="0" max="2" step="0.1" />
                </div>
              </div>

              <section class="control-card agent-paths">
                <div class="path-row"><span>Config</span><code id="agentConfigPath">-</code></div>
                <div class="path-row"><span>Workspace</span><code id="agentWorkspacePath">-</code></div>
                <div class="path-row"><span>Memory</span><code id="agentMemoryPath">-</code></div>
                <div class="path-row"><span>Icons</span><code id="agentIconsPath">-</code></div>
              </section>

              <div class="agent-actions">
                <button id="agentFormReload" class="ghost" type="button">Reload</button>
                <button id="agentSave" class="primary" type="submit">Save Config</button>
              </div>
            </form>
          </section>
        </main>
      </div>
    </section>

    <section id="scheduleTab" class="tab-panel agent-panel schedule-panel" role="tabpanel" aria-label="Scheduled Tasks">
      <div class="agent-landing schedule-landing">
        <aside class="agent-side">
          <section class="agent-hero"><h2 id="scheduleHeroTitle">定时任务</h2><p id="scheduleHeroText">分页管理定时任务；结果是低权重通知，会等待主Agent回合结束后再投递。</p></section>
          <section class="control-card">
            <div class="field"><label for="scheduleAgentSelect"><span>Agent</span><span id="scheduleAgentLabel">任务归属</span></label><select id="scheduleAgentSelect"></select></div>
            <div class="button-row"><button id="scheduleReload" class="ghost" type="button">刷新</button></div>
          </section>
          <section class="control-card schedule-list-card"><div class="schedule-list-head"><strong id="scheduleListTitle">任务列表</strong><span id="schedulePageInfo">-</span></div><div id="scheduleList" class="schedule-list"></div><div class="button-row"><button id="schedulePrev" class="ghost" type="button">上一页</button><button id="scheduleNext" class="ghost" type="button">下一页</button></div></section>
          <section class="note-card"><strong id="scheduleRuleTitle">投递规则</strong><br /><span id="scheduleRuleText">默认投递到发起任务的当前会话；可选指定会话、当前Agent全部普通会话或专属通知会话。通知默认不进入主Agent推理上下文。</span></section>
        </aside>
        <main class="agent-main">
          <header class="agent-main-head"><div class="title-block"><div class="window-dots"><button id="scheduleWinClose" type="button" title="Back to Home"></button><button id="scheduleWinMin" type="button" title="Back to Home"></button><button id="scheduleWinMax" type="button" title="Fullscreen"></button></div><h2 id="scheduleMainTitle">Scheduled Tasks</h2><p id="scheduleState">选择Agent后加载任务列表。</p></div><span class="phase-pill"><span class="phase-dot"></span><span>low-priority</span></span></header>
          <section class="agent-main-body schedule-body">
            <form id="scheduleForm" class="agent-form schedule-form">
              <input id="scheduleTaskId" type="hidden" />
              <div class="agent-form-grid">
                <div class="agent-field"><label for="scheduleTitle"><span id="scheduleLabelTitle">标题</span><span>title</span></label><input id="scheduleTitle" type="text" required /></div>
                <div class="agent-field"><label for="scheduleStatus"><span id="scheduleLabelStatus">状态</span><span>enabled/paused</span></label><select id="scheduleStatus"><option value="enabled">enabled</option><option value="paused">paused</option></select></div>
                <div class="agent-field full"><label for="scheduleContent"><span id="scheduleLabelContent">要做的事情</span><span>task</span></label><textarea id="scheduleContent" required placeholder="写清楚任务目标、输入来源、输出要求..."></textarea></div>
                <div class="agent-field full"><label><span id="scheduleLabelRule">定时规则</span><span>schedule</span></label><div class="schedule-rule-editor"><div class="schedule-rule-type"><button type="button" data-rule-type="daily" class="active" id="scheduleRuleDaily">每天固定时间</button><button type="button" data-rule-type="daily_times" id="scheduleRuleDailyTimes">每天多次</button><button type="button" data-rule-type="daily_window" id="scheduleRuleDailyWindow">每日循环</button><button type="button" data-rule-type="once" id="scheduleRuleOnce">执行一次</button></div><div id="scheduleRuleFields" class="schedule-rule-fields"></div></div><input id="scheduleJson" type="hidden" /></div>
                <div class="agent-field"><label for="scheduleTargetMode"><span id="scheduleLabelTarget">结果投递</span><span>targets</span></label><select id="scheduleTargetMode"><option value="created_session" id="scheduleOptCreated">当前/创建会话</option><option value="session" id="scheduleOptSession">选择会话</option><option value="all_agent_sessions" id="scheduleOptAll">全部普通会话</option><option value="notification_session" id="scheduleOptNotification">Dedicated notification session</option><option value="none" id="scheduleOptNone">不投递</option></select></div>
                <div class="agent-field"><label for="scheduleTargetSessions"><span id="scheduleLabelSessions">会话列表</span><span>multi</span></label><select id="scheduleTargetSessions" multiple></select></div>
              </div>
              <div class="agent-actions"><button id="scheduleSave" class="primary" type="submit">保存任务</button><button id="scheduleNew" class="ghost" type="button">新建</button></div>
            </form>
            <section class="control-card schedule-history-card"><strong id="scheduleHistoryTitle">执行历史</strong><div id="scheduleHistory" class="schedule-history"><span id="scheduleHistoryPlaceholder">选择任务查看历史。</span></div></section>
          </section>
        </main>
      </div>
    </section>

    <section id="skillsTab" class="tab-panel agent-panel skills-panel" role="tabpanel" aria-label="Skills">
      <div class="agent-landing skills-landing">
        <aside class="agent-side">
          <section class="agent-hero"><h2 id="skillsHeroTitle">技能库</h2><p id="skillsHeroText">管理可复用的工作流、最佳实践和领域知识。技能会注入到 Agent 的上下文中，帮助模型更好地完成任务。</p></section>
          <section class="control-card">
            <div class="field"><label for="skillsAgentSelect"><span>Agent</span><span id="skillsAgentLabel">技能归属</span></label><select id="skillsAgentSelect"></select></div>
            <div class="button-row"><button id="skillsReload" class="ghost" type="button">刷新</button><button id="skillsOrganize" class="primary" type="button">整理</button></div>
            <button id="skillsLearnValidate" class="primary skill-learn-button" type="button">Learn + Validate</button>
          </section>
          <section class="control-card skills-list-card"><div class="skills-list-head"><strong id="skillsListTitle">技能列表</strong></div><div id="skillsList" class="skills-list"></div></section>
        </aside>
        <main class="agent-main">
          <header class="agent-main-head"><div class="title-block"><div class="window-dots"><button id="skillsWinClose" type="button" title="Back to Home"></button><button id="skillsWinMin" type="button" title="Back to Home"></button><button id="skillsWinMax" type="button" title="Fullscreen"></button></div><h2 id="skillsMainTitle">Skill Editor</h2><p id="skillsState">选择技能进行编辑，或创建新技能。</p></div><span class="phase-pill"><span class="phase-dot"></span><span>editable</span></span></header>
          <section class="agent-main-body skills-body">
            <form id="skillsForm" class="agent-form skills-form">
              <input id="skillId" type="hidden" />
              <div class="agent-form-grid">
                <div class="agent-field"><label for="skillName"><span id="skillNameLabel">名称</span><span>name</span></label><input id="skillName" type="text" required placeholder="技能名称（如：React组件开发规范）" /></div>
                <div class="agent-field"><label for="skillTags"><span id="skillTagsLabel">标签</span><span>tags</span></label><input id="skillTags" type="text" placeholder="逗号分隔，如: frontend, react, best-practice" /></div>
                <div class="agent-field full"><label for="skillDescription"><span id="skillDescLabel">描述</span><span>description</span></label><input id="skillDescription" type="text" required placeholder="一句话描述这个技能的用途和使用场景" /></div>
                <div class="agent-field full"><label for="skillContent"><span id="skillContentLabel">内容</span><span>content (Markdown)</span></label><textarea id="skillContent" required placeholder="## 概述&#10;&#10;详细的工作流程、最佳实践、代码示例...&#10;&#10;## 步骤&#10;&#10;1. ...&#10;2. ...&#10;&#10;## 示例&#10;&#10;```javascript&#10;// 代码示例&#10;```"></textarea></div>
              </div>
              <div class="agent-actions"><button id="skillSave" class="primary" type="submit">保存技能</button><button id="skillNew" class="ghost" type="button">新建</button><button id="skillExport" class="ghost" type="button">导出</button><button id="skillDelete" class="ghost" type="button" style="color:#ff6b6b;border-color:rgba(255,107,107,.3);">删除</button></div>
            </form>
            <section class="control-card skills-preview-card"><strong id="skillsPreviewTitle">实时预览</strong><div id="skillPreview" class="skill-preview-content"><span class="skill-empty" id="skillEmptyText">填写表单后此处显示预览。</span></div></section>
          </section>
        </main>
      </div>
    </section>

    <section id="settingsTab" class="tab-panel settings-panel" role="tabpanel" aria-label="Settings">
      <div class="agent-landing settings-landing">
        <aside class="agent-side settings-side">
          <section class="agent-hero settings-hero">
            <h2 id="settingsSideTitle">Settings</h2>
            <p id="settingsSideText">Tune Matdance interface language and local preferences.</p>
          </section>
          <nav class="settings-section-nav" aria-label="Settings sections">
            <button class="settings-tag active" type="button" data-settings-section="general">
              <span class="settings-tag-icon">G</span>
              <span class="settings-tag-copy"><span id="settingsNavGeneralTitle" class="settings-tag-title">General</span><span id="settingsNavGeneralSub" class="settings-tag-sub">Language and interface</span></span>
            </button>
            <button class="settings-tag" type="button" data-settings-section="memory">
              <span class="settings-tag-icon">M</span>
              <span class="settings-tag-copy"><span id="settingsNavMemoryTitle" class="settings-tag-title">Memory</span><span id="settingsNavMemorySub" class="settings-tag-sub">Context limits</span></span>
            </button>
            <button class="settings-tag" type="button" data-settings-section="sound">
              <span class="settings-tag-icon">S</span>
              <span class="settings-tag-copy"><span id="settingsNavSoundTitle" class="settings-tag-title">Sound</span><span id="settingsNavSoundSub" class="settings-tag-sub">Agent cues</span></span>
            </button>
            <button class="settings-tag" type="button" data-settings-section="multimodal">
              <span class="settings-tag-icon">A</span>
              <span class="settings-tag-copy"><span id="settingsNavMultiTitle" class="settings-tag-title">Multimodal</span><span id="settingsNavMultiSub" class="settings-tag-sub">Image and audio models</span></span>
            </button>
          </nav>
          <section class="settings-status-card">
            <div id="settingsReserveTitle" class="reserve-title">Settings Status</div>
            <div class="reserve-grid">
              <span id="settingsReserveLanguage" class="reserve-chip">Language</span>
              <span id="settingsReserveTheme" class="reserve-chip">Theme</span>
              <span id="settingsReserveShortcuts" class="reserve-chip">Shortcuts</span>
              <span id="settingsReserveAbout" class="reserve-chip">About</span>
            </div>
          </section>
        </aside>
        <main class="agent-main settings-main">
          <header class="agent-main-head">
            <div class="title-block">
              <div class="window-dots"><button id="settingsWinClose" type="button" title="Back to Home"></button><button id="settingsWinMin" type="button" title="Back to Home"></button><button id="settingsWinMax" type="button" title="Fullscreen"></button></div>
              <h2 id="settingsTitle">Settings</h2>
              <p id="settingsSubtitle">Control global interface options.</p>
            </div>
            <span class="phase-pill"><span class="phase-dot"></span><span id="settingsModeLabel">local settings</span></span>
          </header>
          <section class="agent-main-body settings-body">
            <section id="settingsGeneralSection" class="settings-section-view active" data-settings-section-view="general">
              <div class="settings-section-grid">
                <section class="control-card settings-card settings-language-card">
                  <div class="settings-card-copy">
                    <strong id="languageTitle">Language</strong>
                    <p id="languageDescription">Switch all visible UI text between Chinese and English.</p>
                  </div>
                  <button id="langToggle" class="primary settings-lang-button" type="button">EN</button>
                </section>
                <section class="control-card settings-card settings-language-card">
                  <div class="settings-card-copy">
                    <strong id="settingsGeneralTitle">Interface</strong>
                    <p id="settingsGeneralDesc">Local preferences for this browser. More controls can live here without crowding model configuration.</p>
                  </div>
                  <span class="muted-status" id="settingsGeneralState">Local only</span>
                </section>
                <section class="control-card settings-card settings-language-card">
                  <div class="settings-card-copy">
                    <strong id="privacyAccessTitle">Privacy Access</strong>
                    <p id="privacyAccessDesc">Allow agents to read user-private files only when a task needs it. Keep this off in social apps, mail, forums, unknown pages, or any prompt-injection-heavy environment.</p>
                  </div>
                  <label class="sound-cue-toggle" for="privacyAccessToggle"><span id="privacyAccessToggleLabel">Allow private data access</span><input id="privacyAccessToggle" type="checkbox" /></label>
                  <span class="muted-status" id="privacyAccessState">Default off</span>
                </section>
                <section class="control-card settings-card settings-skill-validation-card">
                  <div class="settings-card-copy">
                    <strong id="skillValidationTitle">Skill Validation</strong>
                    <p id="skillValidationDesc">Queue automatic skill validation globally so idle checks stay low-volume and serial.</p>
                    <label class="sound-cue-toggle" for="skillValidationEnabled"><span id="skillValidationEnabledLabel">Enable automatic validation</span><input id="skillValidationEnabled" type="checkbox" /></label>
                    <span class="muted-status" id="skillValidationState">Every 6 hours, validate 1 skill serially.</span>
                  </div>
                  <div class="settings-memory-limits settings-skill-validation-controls">
                    <div class="field"><label for="skillValidationIntervalHours"><span id="skillValidationIntervalLabel">Interval</span><span id="skillValidationIntervalMeta">hours</span></label><input id="skillValidationIntervalHours" type="number" min="1" max="168" step="1" value="6" /></div>
                    <div class="field"><label for="skillValidationBatchSize"><span id="skillValidationBatchLabel">Batch Size</span><span id="skillValidationBatchMeta">max 3</span></label><select id="skillValidationBatchSize"><option value="1">1</option><option value="2">2</option><option value="3">3</option></select></div>
                  </div>
                </section>
                <section class="control-card settings-card settings-events-card">
                  <div class="settings-card-copy">
                    <strong id="runtimeEventsTitle">Background Events</strong>
                    <p id="runtimeEventsDesc">Recent subagent, scheduler, and recovery events for the selected agent.</p>
                  </div>
                  <div class="settings-events-row">
                    <div class="field">
                      <label for="runtimeEventsAgentSelect"><span>Agent</span><span>events</span></label>
                      <select id="runtimeEventsAgentSelect"></select>
                    </div>
                    <button id="runtimeEventsReload" class="ghost" type="button">Reload</button>
                  </div>
                  <div id="runtimeEventsSummary" class="settings-memory-meter"></div>
                  <div id="runtimeEventsRemaining" class="memory-list"></div>
                  <div id="runtimeEventsList" class="memory-list"></div>
                </section>
              </div>
            </section>
            <section id="settingsMemorySection" class="settings-section-view" data-settings-section-view="memory">
              <div class="settings-memory-layout">
                <section class="control-card settings-card settings-memory-card">
                  <div class="settings-card-copy">
                    <strong id="memoryLimitTitle">Memory Limits</strong>
                    <p id="memoryLimitDesc">Configure token limits for memory organization.</p>
                  </div>
                  <div class="settings-memory-meter" aria-hidden="true">
                    <span><span>hot</span><b id="settingsHotReadout">10000</b></span>
                    <span><span>core</span><b id="settingsCoreReadout">15000</b></span>
                    <span><span>user</span><b id="settingsUserReadout">5000</b></span>
                    <span><span>identity</span><b id="settingsIdentityReadout">2000</b></span>
                  </div>
                </section>
                <section class="control-card settings-card settings-memory-editor">
                  <div class="settings-memory-limits">
                    <div class="field"><label for="settingsHotLimit"><span id="settingsHotLabel">Hot Memory</span><span>tokens</span></label><input id="settingsHotLimit" type="number" min="1000" step="100" value="10000" /></div>
                    <div class="field"><label for="settingsCoreLimit"><span id="settingsCoreLabel">Core Memory</span><span>tokens</span></label><input id="settingsCoreLimit" type="number" min="1000" step="100" value="15000" /></div>
                    <div class="field"><label for="settingsUserLimit"><span id="settingsUserLabel">user.md</span><span>tokens</span></label><input id="settingsUserLimit" type="number" min="500" step="100" value="5000" /></div>
                    <div class="field"><label for="settingsIdentityLimit"><span id="settingsIdentityLabel">identity.md</span><span>tokens</span></label><input id="settingsIdentityLimit" type="number" min="500" step="100" value="2000" /></div>
                  </div>
                  <div class="settings-save-row"><button id="settingsSaveLimits" class="primary" type="button">Save Limits</button></div>
                </section>
              </div>
            </section>
            <section id="settingsSoundSection" class="settings-section-view" data-settings-section-view="sound">
              <div class="settings-sound-layout">
                <section class="control-card settings-card settings-sound-master">
                  <div class="settings-card-copy">
                    <strong id="soundCueTitle">Sound Cues</strong>
                    <p id="soundCueDesc">Short non-voice system sounds for agent state changes and creative markers.</p>
                  </div>
                  <div class="sound-cue-master-controls">
                    <label class="sound-cue-toggle" for="soundCueEnabled"><span id="soundCueEnabledLabel">Enable sound cues</span><input id="soundCueEnabled" type="checkbox" /></label>
                    <div class="sound-cue-volume">
                      <label for="soundCueVolume"><span id="soundCueVolumeLabel">Cue volume</span><span id="soundCueVolumeValue">65%</span></label>
                      <input id="soundCueVolume" type="range" min="0" max="100" step="1" value="65" />
                    </div>
                    <div class="sound-cue-delay">
                      <label for="soundCueDelay"><span id="soundCueDelayLabel">Cue delay</span><span id="soundCueDelayValue">5.0s</span></label>
                      <input id="soundCueDelay" type="range" min="0" max="10000" step="250" value="5000" />
                    </div>
                  </div>
                </section>
                <section class="control-card settings-card sound-cue-list-card">
                  <div class="settings-card-copy">
                    <strong id="soundCueLibraryTitle">Cue Library</strong>
                    <p id="soundCueLibraryDesc">Each type keeps a list; Matdance randomly chooses one when the cue fires.</p>
                  </div>
                  <div class="sound-cue-library-actions">
                    <button id="soundCueImport" class="ghost" type="button">Import</button>
                    <button id="soundCueExport" class="primary" type="button">Export</button>
                  </div>
                  <div id="soundCueList" class="sound-cue-list"></div>
                  <input id="soundCueUploadInput" type="file" accept="audio/*" multiple hidden />
                  <input id="soundCueImportInput" type="file" accept="application/zip,.zip,application/json,.json" hidden />
                </section>
              </div>
            </section>
            <section id="settingsMultimodalSection" class="settings-section-view" data-settings-section-view="multimodal">
              <section class="control-card settings-card multimodal-card">
                <div class="settings-card-copy">
                  <strong id="multiTitle">Multimodal Endpoints</strong>
                  <p id="multiDescription">OpenAI-compatible image, speech, and transcription endpoints. Keys stay write-only; blank keeps the current key.</p>
                </div>
                <div class="multimodal-toolbar">
                  <span id="multiStatus" class="muted-status">Not loaded</span>
                  <div class="button-row">
                    <button id="multiReload" class="ghost" type="button">Reload</button>
                    <button id="multiSave" class="primary" type="button">Save Multimodal</button>
                  </div>
                </div>
                <div class="multimodal-grid">
                  <section class="multimodal-profile">
                    <h3 id="multiGlobalTitle">Global defaults</h3>
                    <div id="multiGlobalForm" class="multimodal-form"></div>
                  </section>
                </div>
              </section>
            </section>
          </section>
        </main>
      </div>
    </section>

    <section id="labTab" class="tab-panel agent-panel lab-panel" role="tabpanel" aria-label="Lab">
      <div class="agent-landing lab-landing">
        <aside class="agent-side lab-side">
          <section class="agent-hero">
            <h2 id="labHeroTitle">Debug Lab</h2>
            <p id="labHeroText">Test real tools and built-in components without asking the agent to perform theater.</p>
          </section>
          <section class="control-card">
            <div class="field"><label for="labAgentSelect"><span>Agent</span><span id="labAgentLabel">test target</span></label><select id="labAgentSelect"></select></div>
            <div class="button-row"><button id="labReload" class="ghost" type="button">Reload</button></div>
          </section>
          <section class="note-card"><strong id="labNoteTitle">Runtime state</strong><br /><span id="labNoteText">These buttons call the same endpoints the agent and chat UI use. Lab results reflect whether the current configuration works.</span></section>
        </aside>
        <main class="agent-main lab-main">
          <header class="agent-main-head">
            <div class="title-block">
              <div class="window-dots"><button id="labWinClose" type="button" title="Back to Home"></button><button id="labWinMin" type="button" title="Back to Home"></button><button id="labWinMax" type="button" title="Fullscreen"></button></div>
              <h2 id="labMainTitle">Lab</h2>
              <p id="labState">Select an agent and test configured multimodal endpoints.</p>
            </div>
            <span class="phase-pill"><span class="phase-dot"></span><span id="labPhaseLabel">debug</span></span>
          </header>
          <section class="agent-main-body lab-body">
            <section class="control-card lab-card lab-image-card">
              <div class="lab-card-head"><strong id="labImageTitle">Image Generation</strong><span id="labImageStatus">image_generation</span></div>
              <textarea id="labImagePrompt" placeholder="Describe the image to generate..."></textarea>
              <div class="lab-row">
                <select id="labImageProfile"><option value="">auto profile</option></select>
                <select id="labImageSize"><option value="">config default</option><option value="1024x1024">1024x1024</option><option value="1024x1536">1024x1536</option><option value="1536x1024">1536x1024</option></select>
                <button id="labImageRun" class="primary" type="button">Generate</button>
              </div>
              <div id="labImageResult" class="lab-result"></div>
            </section>
            <section class="control-card lab-card lab-tts-card">
              <div class="lab-card-head"><strong id="labTtsTitle">Text To Speech</strong><span id="labTtsStatus">text_to_speech</span></div>
              <textarea id="labTtsText" placeholder="Text to speak..."></textarea>
              <div class="lab-row">
                <select id="labTtsProfile"><option value="">auto profile</option></select>
                <input id="labTtsVoice" type="text" placeholder="voice override, optional" />
                <button id="labTtsRun" class="primary" type="button">Speak</button>
              </div>
              <div id="labTtsResult" class="lab-result"></div>
            </section>
            <section class="control-card lab-card lab-stt-card">
              <div class="lab-card-head"><strong id="labSttTitle">Speech To Text</strong><span id="labSttStatus">speech_to_text</span></div>
              <div class="lab-row">
                <button id="labSttRecord" class="ghost" type="button">Record</button>
              </div>
              <pre id="labSttResult" class="lab-transcript">No transcript yet.</pre>
            </section>
          </section>
        </main>
      </div>
    </section>

    <section id="memoryTab" class="tab-panel agent-panel memory-panel" role="tabpanel" aria-label="Memory">
      <div class="agent-landing memory-landing">
        <aside class="agent-side">
          <section class="agent-hero"><h2 id="memoryHeroTitle">记忆管理</h2><p id="memoryHeroText">管理 agent 的核心记忆与长期记忆。</p></section>
          <section class="control-card">
            <div class="field"><label for="memoryAgentSelect"><span>Agent</span><span id="memoryAgentLabel">记忆归属</span></label><select id="memoryAgentSelect"></select></div>
            <div class="button-row"><button id="memoryReload" class="ghost" type="button">刷新</button><button id="memoryOrganize" class="primary" type="button">增量整理</button><button id="memoryOrganizeFull" class="ghost" type="button">全量整理</button></div>
            <p id="memoryOrganizeHint" class="memory-hint">增量整理只处理 bookmark 后的新消息和变更；全量整理会先创建快照，再从全部历史重建。</p>
          </section>
          <section class="control-card memory-nav-card">
            <div class="memory-nav-tabs"><button type="button" data-memory-tab="core" class="active" id="memoryTabCoreBtn">核心记忆</button><button type="button" data-memory-tab="longterm" id="memoryTabLongtermBtn">长期记忆</button><button type="button" data-memory-tab="vector" id="memoryTabVectorBtn">向量记忆</button></div>
          </section>
        </aside>
        <main class="agent-main">
          <header class="agent-main-head"><div class="title-block"><div class="window-dots"><button id="memoryWinClose" type="button" title="Back to Home"></button><button id="memoryWinMin" type="button" title="Back to Home"></button><button id="memoryWinMax" type="button" title="Fullscreen"></button></div><h2 id="memoryMainTitle">Memory</h2><p id="memoryState">选择Agent后加载记忆。</p></div><span class="phase-pill"><span class="phase-dot"></span><span>memory</span></span></header>
          <section class="agent-main-body memory-body">
            <div id="memoryCoreView" class="memory-view active">
              <div class="memory-editors">
                <div class="memory-editor-card"><div class="memory-editor-header"><strong id="memoryLabelUser">user.md</strong><span class="memory-hint" id="memoryHintUser">用户偏好与个人信息</span></div><textarea id="memoryUserMd" placeholder="输入 user.md 内容..."></textarea></div>
                <div class="memory-editor-card"><div class="memory-editor-header"><strong id="memoryLabelIdentity">identity.md</strong><span class="memory-hint" id="memoryHintIdentity">Agent 身份设定</span></div><textarea id="memoryIdentityMd" placeholder="输入 identity.md 内容..."></textarea></div>
                <div class="memory-editor-card"><div class="memory-editor-header"><strong id="memoryLabelHot">Hot Memory</strong><span class="memory-hint" id="memoryHintHot">近期上下文与临时记忆</span></div><textarea id="memoryHotMemory" placeholder="输入热记忆内容..."></textarea></div>
                <div class="memory-editor-card"><div class="memory-editor-header"><strong id="memoryLabelCore">Core Memory</strong><span class="memory-hint" id="memoryHintCore">持久化核心记忆</span></div><textarea id="memoryCoreMemory" placeholder="输入核心记忆内容..."></textarea></div>
              </div>
              <div class="memory-actions"><button id="memorySaveCore" class="primary" type="button">保存核心记忆</button></div>
              <section class="memory-snapshot-panel">
                <div class="memory-editor-header"><strong id="memorySnapshotTitle">Memory Snapshots</strong><span class="memory-hint" id="memorySnapshotHint">全量整理前自动创建，可用于回滚。</span></div>
                <div class="memory-snapshot-actions"><select id="memorySnapshotSelect"></select><button id="memorySnapshotRestore" class="ghost" type="button">恢复快照</button></div>
              </section>
            </div>
            <div id="memoryLongtermView" class="memory-view">
              <div class="memory-ltm-layout">
                <div class="memory-ltm-list">
                  <div class="memory-ltm-search">
                    <input type="date" id="memoryLtmStartDate" />
                    <span class="memory-ltm-search-sep">→</span>
                    <input type="date" id="memoryLtmEndDate" />
                    <button id="memoryLtmResetBtn" class="ghost" type="button">重置</button>
                  </div>
                  <div id="memoryLtmList" class="memory-list"></div>
                  <div class="button-row"><button id="memoryLtmPrev" class="ghost" type="button">上一页</button><button id="memoryLtmNext" class="ghost" type="button">下一页</button></div>
                </div>
                <div class="memory-ltm-preview">
                  <div class="memory-preview-header"><strong id="memoryPreviewTitle">记忆详情</strong><button id="memoryLtmDelete" class="danger" type="button" style="display:none;">删除</button></div>
                  <div id="memoryLtmPreview" class="memory-preview-content">选择一条记忆查看详情。</div>
                </div>
              </div>
            </div>
            <div id="memoryVectorView" class="memory-view">
              <div class="memory-vector-layout">
                <section class="memory-vector-panel">
                  <div class="memory-vector-panel-head"><strong id="memoryVectorResultsTitle">向量搜索</strong><div class="memory-vector-meta" id="memoryVectorSearchMeta"></div></div>
                  <div class="memory-vector-search">
                    <input type="search" id="memoryVectorQuery" autocomplete="off" placeholder="搜索向量记忆..." />
                    <button id="memoryVectorSearchBtn" class="primary" type="button">搜索</button>
                  </div>
                  <div id="memoryVectorResults" class="memory-vector-results"><div class="memory-vector-empty" id="memoryVectorEmpty">输入查询以搜索向量记忆。</div></div>
                </section>
                <section class="memory-vector-panel">
                  <div class="memory-vector-panel-head"><strong id="memoryVectorAtlasTitle">神经元图册</strong><div class="memory-vector-meta" id="memoryVectorMeta"></div></div>
                  <div class="memory-vector-map">
                    <canvas id="memoryVectorCanvas" class="memory-vector-canvas" aria-label="Vector memory neuron atlas"></canvas>
                    <div id="memoryVectorTooltip" class="memory-vector-tooltip"></div>
                  </div>
                </section>
              </div>
            </div>
          </section>
        </main>
      </div>
    </section>
    </div>
  </div>

  <section id="easterEggOverlay" class="easter-egg-overlay" aria-hidden="true">
    <div class="easter-egg-card" role="dialog" aria-modal="true" aria-label="Matdance">
      <p class="easter-egg-quote">夏日消溶，江河横溢，人或为鱼鳖。千秋功罪，谁人曾与评说。--毛泽东</p>
    </div>
  </section>

  <section id="blankPage" class="blank-page" aria-hidden="true">
    <div class="blank-card">
      <h2 id="blankTitle">Test Blank Page</h2>
      <p id="blankText">The window action routed here intentionally.</p>
      <button id="blankBack" class="primary" type="button">Back</button>
    </div>
  </section>

  <script>
    const $ = (id) => document.getElementById(id);
    function preferredLanguage() {
      const stored = localStorage.getItem('matdanceLang');
      if (stored === 'zh' || stored === 'en') return stored;
      const languages = Array.isArray(navigator.languages) && navigator.languages.length ? navigator.languages : [navigator.language || navigator.userLanguage || ''];
      return languages.some(lang => String(lang || '').toLowerCase().startsWith('zh')) ? 'zh' : 'en';
    }
    const state = { agents: [], sessions: [], agent: null, session: null, sessionReadOnly: false, busy: false, abortController: null, matrixTimer: null, commandIndex: 0, activeTab: 'home', settingsSection: 'general', runtimeEventsTimer: null, runtimeEventsAgent: null, schedulePage: 1, schedulePageSize: 8, scheduledTasks: null, scheduledSelected: null, lang: preferredLanguage(), memoryTab: 'core', memoryPage: 1, memoryPageSize: 10, memoryItems: null, memorySelectedItem: null, memorySnapshots: [], memoryVectorAtlas: null, memoryVectorResults: null, memoryVectorHover: null, memoryVectorPinned: null, memoryOrganizing: false, skillsWorking: false, skillsLearnFiles: [], selectedSkillValidationReport: null, selectedSkillImportReport: null, browserOpen: false, browserWs: null, browserMaximized: false, chatNearBottom: true, chatFollowStream: true, chatUserScrollIntentAt: 0, chatProgrammaticScrollUntil: 0, suppressChatAutoScroll: false, chatAttachments: [], multimodal: null, securitySettings: null, skillValidationSettings: null, modelProviders: [], audioPlayer: null, audioUrl: null, audioButton: null, ttsErrorTimer: null, soundCues: null, soundCueGroup: 'flow', soundCuePlayer: null, soundCueQueue: [], soundCuePlaying: false, soundCuePrimed: false, soundCueBlocked: false, soundCueUploadType: null, soundCueSaveTimer: null, soundCueLastPlayedAt: {}, soundCueIdleResolvers: [], scheduledNoticeKeys: new Set(), imageNoticeKeys: new Set(), hostNoticeContinuationRunning: false, recorder: null, recordChunks: [], voiceMode: false, voiceRecording: false, voiceCanceled: false, voiceStartY: 0, voiceStartAt: 0, voiceTimer: null, voiceSession: null, voicePointerId: null, labRecorder: null, labRecordChunks: [] };
    const APP_DESIGN_WIDTH = 1360;
    const APP_DESIGN_HEIGHT = 995;
    function updateAppScale() {
      const stage = document.querySelector('.app-scale-stage');
      if (!stage) return;
      if (window.matchMedia('(max-width: 980px)').matches) {
        document.documentElement.style.setProperty('--app-scale', '1');
        return;
      }
      const scale = Math.max(0.1, Math.min(stage.clientWidth / APP_DESIGN_WIDTH, stage.clientHeight / APP_DESIGN_HEIGHT));
      document.documentElement.style.setProperty('--app-scale', scale.toFixed(4));
    }
    updateAppScale();
    window.addEventListener('resize', updateAppScale);
    const easterEgg = { clicks: 0, lastClickAt: 0, windowMs: 1500 };
    const i18n = {
      zh: {
        connected: '本地 Web UI 已连接。',
        busyHint: 'Agent 正在处理，输入将在本轮结束后恢复。',
        idle: 'idle',
        inputPlaceholder: '输入消息，让 agent 开始处理...',
        send: '发送',
        you: 'You',
        tool: 'Tool',
        toolResult: '工具结果',
        streaming: 'streaming',
        complete: 'complete',
        scheduledNotice: '定时任务通知',
        noVisible: '(没有可见的助手文本)',
        noAgents: '没有发现 agent。请在 Agent 页面新建一个。',
        sessionReady: '新的会话已经准备好。发送第一条消息后，agent 卡片和工具卡片会在这里流式出现。',
        newSession: 'New Session',
        refresh: 'Refresh',
        reload: 'Reload',
        save: 'Save',
        saveConfig: 'Save Config',
        newAgent: 'New Agent',
        deleteAgent: 'Delete',
        uploadAvatar: 'Upload Avatar',
        avatarHint: '',
        newAgentPrompt: '请输入新 agent 名称：',
        deleteConfirm: '确认删除当前 agent 及其本地文件？',
        chooseFile: '请先选择头像文件。',
        loaded: '已加载',
        saved: '已保存',
        loadingConfig: '正在加载 agent 配置...',
        savingConfig: '正在保存 agent 配置...'
      },
      en: {
        connected: 'Local Web UI connected.',
        busyHint: 'Agent is working; input returns after this turn.',
        idle: 'idle',
        inputPlaceholder: 'Type a message to start the agent...',
        send: 'Send',
        you: 'You',
        tool: 'Tool',
        toolResult: 'Tool Result',
        streaming: 'streaming',
        complete: 'complete',
        scheduledNotice: 'Scheduled Task Notice',
        noVisible: '(no visible assistant text)',
        noAgents: 'No agents found. Create one from the Agent page.',
        sessionReady: 'The new session is ready. Agent messages and tool cards will stream here.',
        newSession: 'New Session',
        refresh: 'Refresh',
        reload: 'Reload',
        save: 'Save',
        saveConfig: 'Save Config',
        newAgent: 'New Agent',
        deleteAgent: 'Delete',
        uploadAvatar: 'Upload Avatar',
        avatarHint: '',
        newAgentPrompt: 'Enter a new agent name:',
        deleteConfirm: 'Delete the current agent and local files?',
        chooseFile: 'Choose an avatar file first.',
        loaded: 'Loaded',
        saved: 'Saved',
        loadingConfig: 'Loading agent config...',
        savingConfig: 'Saving agent config...',
        configured: 'Configured',
        missing: 'Missing',
        ready: 'Ready',
        empty: 'Empty'
      }
    };
    Object.assign(i18n.zh,{idle:'空闲',newSession:'新会话',refresh:'刷新',reload:'重新载入',save:'保存',saveConfig:'保存配置',newAgent:'新建 Agent',deleteAgent:'删除',uploadAvatar:'上传头像',avatarHint:'',loaded:'已加载',saved:'已保存',loadingConfig:'正在加载 agent 配置...',savingConfig:'正在保存 agent 配置...',configured:'已配置',missing:'缺失',ready:'就绪',empty:'空',langToggle:'English',homeEyebrow:'Matrix Dance 星图',homeSubtitle:'选择着陆地点...',hudAwaiting:'等待目标',hudChoose:'选择一个星球',hudHelp:'拖动旋转星图，滚轮前进或后退，右键拖动横移。悬停星球查看目的地，点击后跃迁进入页面。',hudLanding:'着陆目标',hudReserved:'预留轨道',homeCtrlRotate:'左键拖动：旋转摄像机',homeCtrlZoom:'滚轮：前进 / 后退',homeCtrlPan:'右键拖动：横移',homeCtrlReset:'静止 3 秒：回归原点'});
    Object.assign(i18n.en,{langToggle:'中文',homeEyebrow:'Matrix Dance Star Map',homeSubtitle:'Select landing zone...',hudAwaiting:'Awaiting target',hudChoose:'Choose a planet',hudHelp:'Drag to rotate the star map, scroll to move forward or backward, and right-drag to pan. Hover a planet for details, then click to warp into the page.',hudLanding:'Landing target',hudReserved:'Reserved orbit',homeCtrlRotate:'Left drag: rotate camera',homeCtrlZoom:'Wheel: forward / backward',homeCtrlPan:'Right drag: pan',homeCtrlReset:'Idle 3s: return home'});
    Object.assign(i18n.zh,{tagChatName:'Chat',tagChatDescription:'流式对话、工具调用、任务进度与 Markdown 消息中心。',tagAgentName:'Agent',tagAgentDescription:'配置模型、API、上下文窗口、输出限制和本地 agent 状态。',tagSettingsName:'Settings',tagSettingsDescription:'语言切换、显示偏好和本地界面体验设置。',tagSkillsName:'Skills',tagSkillsDescription:'可复用工作流、最佳实践和领域知识技能库。',tagMemoryName:'Memory',tagMemoryDescription:'预留：hot/core memory、检索和长期记忆维护。',reserved:'预留',brandSubtitle:'本地 C# Agent 控制台',chipStreaming:'流式',chipTools:'工具',chipMemory:'记忆',agentLabel:'Agent',profileLabel:'配置档案',sessionLabel:'会话',timelineLabel:'时间线',metricModel:'模型',metricContext:'上下文',metricMessages:'消息',metricToolCalls:'工具调用',noteTitle:'交互',noteText:'Enter 发送，Shift + Enter 换行。工具调用会嵌在 AI 卡片内，流式响应不会打断界面。Web 模式默认禁用交互式 bash，避免服务端阻塞确认。',titleReady:'准备就绪',subtitleReady:'选择 agent 并开始聊天。'});
    Object.assign(i18n.en,{tagChatName:'Chat',tagChatDescription:'Streaming chat, tool calls, task progress, and Markdown messages.',tagAgentName:'Agent',tagAgentDescription:'Configure model, API, context window, output limits, and local agent state.',tagSettingsName:'Settings',tagSettingsDescription:'Language switching, display preferences, and local interface options.',tagSkillsName:'Skills',tagSkillsDescription:'Reusable workflows, best practices, and domain knowledge skill library.',tagMemoryName:'Memory',tagMemoryDescription:'Reserved: hot/core memory, retrieval, and long-term memory maintenance.',reserved:'reserved',brandSubtitle:'Local C# Agent Console',chipStreaming:'Streaming',chipTools:'Tools',chipMemory:'Memory',agentLabel:'Agent',profileLabel:'profile',sessionLabel:'Session',timelineLabel:'timeline',metricModel:'Model',metricContext:'Context',metricMessages:'Messages',metricToolCalls:'Tool Calls',noteTitle:'Interaction',noteText:'Enter sends, Shift + Enter inserts a newline. Tool calls stay inside the AI card, and streaming responses do not interrupt the UI. Web mode disables interactive bash by default to avoid blocking server confirmations.',titleReady:'Ready',subtitleReady:'Choose an agent and start chatting.'});
    Object.assign(i18n.zh,{agentCenterTitle:'Agent 中心',agentCenterText:'配置本地 agent、模型、API 和头像。API Key 不会明文回填；留空即可保留现有值。',configTargetLabel:'配置目标',agentAvatarTitle:'Agent 头像',agentStatusSessions:'会话',agentStatusApiKey:'API Key',agentStatusHotMemory:'Hot Memory',agentStatusCoreMemory:'Core Memory',reservedTabs:'预留 Tags',reservedWorkspace:'Workspace',reservedMemory:'Memory',reservedTools:'Tools',reservedRuns:'Runs',reservedSettings:'Settings',agentConfigTitle:'Agent 配置',agentConfigInitial:'选择一个 agent 以载入配置。',localConfig:'本地配置',nameLabel:'名称',folderIdLabel:'文件夹 ID',identityHelp:'Agent 身份跟随本地文件夹名称。',apiTypeLabel:'API 类型',providerLabel:'供应商',baseUrlLabel:'Base URL',endpointLabel:'端点',modelLabel:'模型',modelIdLabel:'model_id',apiKeyLabel:'API Key',hiddenOnLoadLabel:'载入时隐藏',apiKeyPlaceholder:'留空以保留当前 key',contextLabel:'上下文',tokensLabel:'tokens',maxOutputLabel:'最大输出',maxConcurrencyLabel:'最大并发数',backgroundSlotsLabel:'Agent 预算',temperatureLabel:'温度',pathConfig:'配置',pathWorkspace:'工作区',pathMemory:'记忆',pathIcons:'头像'});
    Object.assign(i18n.en,{agentCenterTitle:'Agent Center',agentCenterText:'Configure the selected local agent, model, API, and avatar. API keys stay hidden; leaving the key field blank keeps the existing value.',configTargetLabel:'config target',agentAvatarTitle:'Agent Avatar',agentStatusSessions:'Sessions',agentStatusApiKey:'API Key',agentStatusHotMemory:'Hot Memory',agentStatusCoreMemory:'Core Memory',reservedTabs:'Reserved Tags',reservedWorkspace:'Workspace',reservedMemory:'Memory',reservedTools:'Tools',reservedRuns:'Runs',reservedSettings:'Settings',agentConfigTitle:'Agent Configuration',agentConfigInitial:'Select an agent to load configuration.',localConfig:'local config',nameLabel:'Name',folderIdLabel:'folder id',identityHelp:'Agent identity follows the local folder name.',apiTypeLabel:'API Type',providerLabel:'provider',baseUrlLabel:'Base URL',endpointLabel:'endpoint',modelLabel:'Model',modelIdLabel:'model_id',apiKeyLabel:'API Key',hiddenOnLoadLabel:'hidden on load',apiKeyPlaceholder:'Leave blank to keep current key',contextLabel:'Context',tokensLabel:'tokens',maxOutputLabel:'Max Output',maxConcurrencyLabel:'Max Concurrency',backgroundSlotsLabel:'agent budget',temperatureLabel:'Temperature',pathConfig:'Config',pathWorkspace:'Workspace',pathMemory:'Memory',pathIcons:'Icons'});
    Object.assign(i18n.zh,{skillsSideTitle:'技能库',skillsSideText:'管理可复用的工作流、最佳实践和领域知识。技能会注入到 Agent 的上下文中，帮助模型更好地完成任务。',skillsListTitle:'技能列表',skillsAgentLabel:'技能归属',skillsMainTitle:'Skill Editor',skillsState:'选择技能进行编辑，或创建新技能。',skillsPreviewTitle:'实时预览',skillsEmpty:'暂无技能。创建第一个技能来记录可复用的工作流。',skillsEmptyPreview:'填写表单后此处显示预览。',skillsNameLabel:'名称',skillsTagsLabel:'标签',skillsDescLabel:'描述',skillsContentLabel:'内容',skillsSave:'保存技能',skillsNew:'新建',skillsExport:'导出',skillsDelete:'删除',noDescription:'无描述',settingsSideTitle:'Settings',settingsSideText:'调整 Matdance 界面语言、显示偏好和本地体验。',settingsReserveTitle:'预留设置',settingsReserveLanguage:'语言',settingsReserveTheme:'主题',settingsReserveShortcuts:'快捷键',settingsReserveAbout:'关于',settingsTitle:'Settings',settingsSubtitle:'管理全局界面选项。',settingsModeLabel:'本地设置',languageTitle:'语言',languageDescription:'默认跟随浏览器/系统语言；点这里可以在中文与 English 之间切换。',memoryLimitTitle:'记忆容量限制',memoryLimitDesc:'配置记忆整理时的 token 上限。',memoryLimitHot:'热记忆上限',memoryLimitCore:'核心记忆上限',memoryLimitUser:'user.md 上限',memoryLimitIdentity:'identity.md 上限',memoryLimitSave:'保存限制',blankTitle:'测试空白页',blankText:'窗口动作会先跳转到这里。',blankBack:'返回',backToHomeTitle:'返回首页',fullscreenTitle:'全屏',sessionWord:'会话',messagesShort:'条消息',steps:'步骤',activeTask:'活跃任务',noActiveTask:'暂无活跃任务。',notFilledStep:'未填写步骤',loadFailedPrefix:'加载失败',refreshFailedPrefix:'刷新失败',saveFailedPrefix:'保存失败'});
    Object.assign(i18n.en,{skillsSideTitle:'Skills',skillsSideText:'Manage reusable workflows, best practices, and domain knowledge. Skills are injected into the agent context to help the model perform better.',skillsListTitle:'Skill List',skillsAgentLabel:'Skill Owner',skillsMainTitle:'Skill Editor',skillsState:'Select a skill to edit, or create a new one.',skillsPreviewTitle:'Live Preview',skillsEmpty:'No skills yet. Create the first skill to record a reusable workflow.',skillsEmptyPreview:'Preview will appear here after filling the form.',skillsNameLabel:'Name',skillsTagsLabel:'Tags',skillsDescLabel:'Description',skillsContentLabel:'Content',skillsSave:'Save Skill',skillsNew:'New',skillsExport:'Export',skillsDelete:'Delete',noDescription:'No description',settingsSideTitle:'Settings',settingsSideText:'Tune Matdance interface language, display preferences, and local experience.',settingsReserveTitle:'Reserved Settings',settingsReserveLanguage:'Language',settingsReserveTheme:'Theme',settingsReserveShortcuts:'Shortcuts',settingsReserveAbout:'About',settingsTitle:'Settings',settingsSubtitle:'Control global interface options.',settingsModeLabel:'local settings',languageTitle:'Language',languageDescription:'Defaults to your browser/system language; click here to switch between English and Chinese.',memoryLimitTitle:'Memory Limits',memoryLimitDesc:'Configure token limits for memory organization.',memoryLimitHot:'Hot Memory Limit',memoryLimitCore:'Core Memory Limit',memoryLimitUser:'user.md Limit',memoryLimitIdentity:'identity.md Limit',memoryLimitSave:'Save Limits',blankTitle:'Test Blank Page',blankText:'The window action routed here intentionally.',blankBack:'Back',backToHomeTitle:'Back to Home',fullscreenTitle:'Fullscreen',sessionWord:'Session',messagesShort:'msgs',steps:'steps',activeTask:'Active task',noActiveTask:'No active task.',notFilledStep:'No step text',loadFailedPrefix:'Load failed',refreshFailedPrefix:'Refresh failed',saveFailedPrefix:'Save failed'});
    Object.assign(i18n.zh,{cmdNewSessionDesc:'创建一个新的会话',cmdNewSessionDone:'已创建新会话。',cmdRefreshDesc:'刷新 Agent 与 Session 状态',cmdRefreshDone:'已刷新列表和状态。',cmdStatusDesc:'重新载入当前会话状态',cmdStatusDone:'当前会话状态已更新。',cmdClearDesc:'清空当前聊天视图但保留历史',cmdClearEmpty:'当前视图已清空；刷新或切换会话可重新载入历史。',cmdClearDone:'聊天视图已清空。',cmdHomeDesc:'返回 Matdance 首页',cmdHomeDone:'已返回首页。',cmdAgentDesc:'切换到 Agent 配置页',cmdAgentDone:'已切换到 Agent 配置页。',cmdSettingsDesc:'切换到 Settings 设置页',cmdSettingsDone:'已切换到 Settings。',cmdChatDesc:'切换回 Chat 页面',cmdChatDone:'已切换到 Chat。',cmdMemoryDesc:'切换到 Memory 记忆页',cmdMemoryDone:'已切换到 Memory。',cmdHelpDesc:'显示支持的命令',cmdHelpTitle:'支持的斜杠命令',cmdHelpDone:'命令帮助已显示。',commandNoCommand:'没有匹配命令',commandNoCommandHint:'输入 / 查看支持的命令',commandCompleteHint:'{command} · Enter 执行，继续输入可作为命令参数。',unknownCommand:'未知命令 {command}。输入 / 查看支持的命令。',commandBusy:'Agent 正在处理，本轮结束后再执行命令。'});
    Object.assign(i18n.en,{cmdNewSessionDesc:'Create a new session',cmdNewSessionDone:'New session created.',cmdRefreshDesc:'Refresh Agent and Session state',cmdRefreshDone:'Lists and status refreshed.',cmdStatusDesc:'Reload the current session state',cmdStatusDone:'Current session state updated.',cmdClearDesc:'Clear the current chat view while keeping history',cmdClearEmpty:'Current view cleared; refresh or switch sessions to reload history.',cmdClearDone:'Chat view cleared.',cmdHomeDesc:'Return to the Matdance home page',cmdHomeDone:'Returned home.',cmdAgentDesc:'Switch to Agent configuration',cmdAgentDone:'Switched to Agent configuration.',cmdSettingsDesc:'Switch to Settings',cmdSettingsDone:'Switched to Settings.',cmdChatDesc:'Switch back to Chat',cmdChatDone:'Switched to Chat.',cmdMemoryDesc:'Switch to Memory page',cmdMemoryDone:'Switched to Memory.',cmdHelpDesc:'Show supported commands',cmdHelpTitle:'Supported slash commands',cmdHelpDone:'Command help displayed.',commandNoCommand:'No command',commandNoCommandHint:'Type / to view supported commands',commandCompleteHint:'{command} · Press Enter to run; keep typing to pass command arguments.',unknownCommand:'Unknown command {command}. Type / to view supported commands.',commandBusy:'Agent is working; run commands after this turn finishes.'});
    Object.assign(i18n.zh,{cmdNewSessionDesc:'创建一个新的会话',cmdNewSessionDone:'已创建新会话。',cmdRefreshDesc:'刷新 Agent 与 Session 状态',cmdRefreshDone:'已刷新列表和状态。',cmdStatusDesc:'重新载入当前会话状态',cmdStatusDone:'当前会话状态已更新。',cmdClearDesc:'清空当前聊天视图但保留历史',cmdClearEmpty:'当前视图已清空；刷新或切换会话可重新载入历史。',cmdClearDone:'聊天视图已清空。',cmdHomeDesc:'返回 Matdance 首页',cmdHomeDone:'已返回首页。',cmdAgentDesc:'切换到 Agent 配置页',cmdAgentDone:'已切换到 Agent 配置页。',cmdSettingsDesc:'切换到 Settings 设置页',cmdSettingsDone:'已切换到 Settings。',cmdChatDesc:'切换回 Chat 页面',cmdChatDone:'已切换到 Chat。',cmdHelpDesc:'显示支持的命令',cmdHelpTitle:'支持的斜杠命令',cmdHelpDone:'命令帮助已显示。',commandNoCommand:'没有匹配命令',commandNoCommandHint:'输入 / 查看支持的命令',commandCompleteHint:'{command} · Enter 执行，继续输入可作为命令参数。',unknownCommand:'未知命令 {command}。输入 / 查看支持的命令。',commandBusy:'Agent 正在处理，本轮结束后再执行命令。'});
    Object.assign(i18n.en,{cmdNewSessionDesc:'Create a new session',cmdNewSessionDone:'New session created.',cmdRefreshDesc:'Refresh Agent and Session state',cmdRefreshDone:'Lists and status refreshed.',cmdStatusDesc:'Reload the current session state',cmdStatusDone:'Current session state updated.',cmdClearDesc:'Clear the current chat view while keeping history',cmdClearEmpty:'Current view cleared; refresh or switch sessions to reload history.',cmdClearDone:'Chat view cleared.',cmdHomeDesc:'Return to the Matdance home page',cmdHomeDone:'Returned home.',cmdAgentDesc:'Switch to Agent configuration',cmdAgentDone:'Switched to Agent configuration.',cmdSettingsDesc:'Switch to Settings',cmdSettingsDone:'Switched to Settings.',cmdChatDesc:'Switch back to Chat',cmdChatDone:'Switched to Chat.',cmdHelpDesc:'Show supported commands',cmdHelpTitle:'Supported slash commands',cmdHelpDone:'Command help displayed.',commandNoCommand:'No command',commandNoCommandHint:'Type / to view supported commands',commandCompleteHint:'{command} · Press Enter to run; keep typing to pass command arguments.',unknownCommand:'Unknown command {command}. Type / to view supported commands.',commandBusy:'Agent is working; run commands after this turn finishes.'});
    Object.assign(i18n.zh,{temperatureRangeLabel:'0 - 2'});
    Object.assign(i18n.en,{temperatureRangeLabel:'0 - 2'});
    Object.assign(i18n.zh,{toolsLabel:'\u5de5\u5177',waitingOutput:'\u7b49\u5f85\u8f93\u51fa...'});
    Object.assign(i18n.en,{toolsLabel:'Tools',waitingOutput:'waiting for output...'});
    Object.assign(i18n.zh,{thinkingBlock:'\u601d\u8003',thinkingComplete:'\u601d\u8003\u5b8c\u6210'});
    Object.assign(i18n.en,{thinkingBlock:'Thinking',thinkingComplete:'Thinking complete'});
    Object.assign(i18n.zh,{stopResponse:'\u505c\u6b62',stopped:'\u5df2\u505c\u6b62',stopping:'\u6b63\u5728\u505c\u6b62...',stoppedMessage:'\u54cd\u5e94\u5df2\u624b\u52a8\u505c\u6b62\u3002'});
    Object.assign(i18n.en,{stopResponse:'Stop',stopped:'stopped',stopping:'stopping...',stoppedMessage:'Response stopped manually.'});
    Object.assign(i18n.zh,{attachFiles:'添加附件',filesEmpty:'文件区：最多 3 个附件',fileLimit:'最多只能添加 3 个附件。',fileUnsupported:'不支持的附件类型',fileDuplicate:'已忽略重复附件',fileTooLarge:'附件过大',fileTotalTooLarge:'附件总大小超过限制',fileImage:'图片',fileDocument:'文档',fileArchive:'压缩包',fileAttachment:'附件'});
    Object.assign(i18n.en,{attachFiles:'Attach files',filesEmpty:'Files: up to 3 attachments',fileLimit:'At most 3 attachments are allowed.',fileUnsupported:'Unsupported attachment type',fileDuplicate:'Duplicate attachment ignored',fileTooLarge:'Attachment is too large',fileTotalTooLarge:'Total attachment size is too large',fileImage:'Image',fileDocument:'Document',fileArchive:'Archive',fileAttachment:'Attachment'});
    Object.assign(i18n.zh,{tagScheduleName:'Schedule',tagScheduleDescription:'定时任务列表、执行历史、toolcall轨迹和低权重结果通知。',
      scheduleTitle:'定时任务',scheduleSubtitle:'分页管理定时任务；结果是低权重通知，会等待主Agent回合结束后再投递。',scheduleListTitle:'任务列表',scheduleQueueLabel:'QUEUE',scheduleTraceLabel:'TRACE',scheduleTaskSpecLabel:'TASK SPEC',
      scheduleReload:'刷新',schedulePrev:'上一页',scheduleNext:'下一页',scheduleSaveTask:'保存任务',scheduleNewTask:'新建',
      scheduleDeliveryRule:'投递规则',scheduleDeliveryRuleText:'默认投递到发起任务的当前会话；可选指定会话、当前Agent全部普通会话或专属通知会话。通知默认不进入主Agent推理上下文。',
      scheduleStatusLabel:'状态',scheduleTitleLabel:'标题',scheduleContentLabel:'要做的事情',scheduleScheduleLabel:'定时规则',scheduleTargetsLabel:'结果投递',scheduleSessionsLabel:'会话列表',
      scheduleTargetCreated:'当前/创建会话',scheduleTargetSession:'选择会话',scheduleTargetAll:'全部普通会话',scheduleTargetNone:'不投递',
      scheduleRuleDaily:'每天固定时间',scheduleRuleDailyTimes:'每天多次',scheduleRuleDailyWindow:'每日循环',scheduleRuleOnce:'执行一次',
      scheduleStateInit:'选择Agent后加载任务列表。',scheduleStateLoading:'加载中...',scheduleStateNew:'新任务',
      scheduleHistoryTitle:'执行历史',scheduleHistoryEmpty:'暂无执行历史',scheduleHistoryPlaceholder:'选择任务查看历史。',scheduleNoTasks:'暂无任务',
      scheduleConfirmDelete:'确认删除此定时任务？',scheduleConfirmRun:'确认立即测试一次此任务？测试会真实执行并投递结果，但不会推进原定 nextRunAt。',
      scheduleStatusEnabled:'已启用',scheduleStatusPaused:'已暂停',scheduleStatusCompleted:'已完成',
      scheduleNextRun:'下次执行',scheduleLastRun:'上次执行',scheduleRunCount:'执行次数',scheduleFailCount:'失败次数',
      memoryTitle:'记忆管理',memorySubtitle:'管理 agent 的核心记忆与长期记忆。',memoryAgentLabel:'记忆归属',
      memoryReload:'刷新',memoryOrganize:'增量整理',memoryOrganizeFull:'全量整理',memoryOrganizeHint:'增量整理只处理 bookmark 后的新消息和变更；全量整理会先创建快照，再从全部历史重建。',memoryTabCore:'核心记忆',memoryTabLongterm:'长期记忆',memoryTabVector:'向量记忆',
      memoryMainTitle:'Memory',memoryStateInit:'选择Agent后加载记忆。',memoryStateLoading:'加载中...',
      memoryLabelUser:'user.md',memoryHintUser:'用户偏好与个人信息',memoryLabelIdentity:'identity.md',memoryHintIdentity:'Agent 身份设定',
      memoryLabelHot:'Hot Memory',memoryHintHot:'近期上下文与临时记忆',memoryLabelCore:'Core Memory',memoryHintCore:'持久化核心记忆',memorySaveCore:'保存核心记忆',
      memorySnapshotTitle:'记忆快照',memorySnapshotHint:'全量整理前自动创建，可在结果不理想时回滚。',memorySnapshotRestore:'恢复快照',memorySnapshotEmpty:'暂无快照',memorySnapshotConfirm:'确认恢复这个记忆快照？当前记忆文件会被覆盖。',memorySnapshotRestored:'快照已恢复',
      memoryLtmSearchPlaceholder:'搜索长期记忆...',memoryLtmSearchBtn:'搜索',memoryLtmPrev:'上一页',memoryLtmNext:'下一页',
      memoryPreviewTitle:'记忆详情',memoryLtmEmpty:'暂无长期记忆',memoryLtmSelectHint:'选择一条记忆查看详情。',memoryLtmDelete:'删除',
      memoryConfirmDelete:'确认删除此条长期记忆？'
    });
    Object.assign(i18n.en,{tagScheduleName:'Schedule',tagScheduleDescription:'Scheduled task list, run history, tool-call trace, and low-weight notifications.',
      scheduleTitle:'Scheduled Tasks',scheduleSubtitle:'Paginated scheduled task manager. Results are low-weight notices delivered after the main agent turn.',
      scheduleListTitle:'Task List',scheduleQueueLabel:'QUEUE',scheduleTraceLabel:'TRACE',scheduleTaskSpecLabel:'TASK SPEC',
      scheduleReload:'Reload',schedulePrev:'Prev',scheduleNext:'Next',scheduleSaveTask:'Save Task',scheduleNewTask:'New',
      scheduleDeliveryRule:'Delivery Rules',scheduleDeliveryRuleText:'Default: deliver to the current session that creates the task. You can also choose a specific session, all normal sessions of this agent, or a dedicated notification session. Notices do NOT enter main agent context by default.',
      scheduleStatusLabel:'Status',scheduleTitleLabel:'Title',scheduleContentLabel:'Task Content',scheduleScheduleLabel:'Schedule Rule',scheduleTargetsLabel:'Delivery Target',scheduleSessionsLabel:'Sessions',
      scheduleTargetCreated:'Current/Created Session',scheduleTargetSession:'Select Session',scheduleTargetAll:'All Normal Sessions',scheduleTargetNone:'None',
      scheduleRuleDaily:'Daily at fixed time',scheduleRuleDailyTimes:'Multiple times daily',scheduleRuleDailyWindow:'Daily window',scheduleRuleOnce:'Run once',
      scheduleStateInit:'Select an agent to load tasks.',scheduleStateLoading:'Loading...',scheduleStateNew:'New Task',
      scheduleHistoryTitle:'Execution History',scheduleHistoryEmpty:'No execution history',scheduleHistoryPlaceholder:'Select a task to view history.',scheduleNoTasks:'No tasks yet',
      scheduleConfirmDelete:'Delete this scheduled task?',scheduleConfirmRun:'Test this task now? The test does real work and delivers results, but will not advance the original nextRunAt.',
      scheduleStatusEnabled:'Enabled',scheduleStatusPaused:'Paused',scheduleStatusCompleted:'Completed',
      scheduleNextRun:'Next Run',scheduleLastRun:'Last Run',scheduleRunCount:'Runs',scheduleFailCount:'Fails',
      memoryTitle:'Memory',memorySubtitle:'Manage agent core and long-term memories.',memoryAgentLabel:'Memory Owner',
      memoryReload:'Reload',memoryOrganize:'Incremental',memoryOrganizeFull:'Full rebuild',memoryOrganizeHint:'Incremental processes new or changed messages after bookmarks. Full rebuild snapshots memory first, then rebuilds from all history.',memoryTabCore:'Core Memory',memoryTabLongterm:'Long-term Memory',memoryTabVector:'Vector Memory',
      memoryMainTitle:'Memory',memoryStateInit:'Select an agent to load memories.',memoryStateLoading:'Loading...',
      memoryLabelUser:'user.md',memoryHintUser:'User preferences and personal info',memoryLabelIdentity:'identity.md',memoryHintIdentity:'Agent identity settings',
      memoryLabelHot:'Hot Memory',memoryHintHot:'Recent context and temporary memory',memoryLabelCore:'Core Memory',memoryHintCore:'Persistent core memory',memorySaveCore:'Save Core Memory',
      memorySnapshotTitle:'Memory Snapshots',memorySnapshotHint:'Created before full rebuilds. Restore one if the result is not acceptable.',memorySnapshotRestore:'Restore Snapshot',memorySnapshotEmpty:'No snapshots',memorySnapshotConfirm:'Restore this memory snapshot? Current memory files will be overwritten.',memorySnapshotRestored:'Snapshot restored',
      memoryLtmSearchPlaceholder:'Search long-term memories...',memoryLtmSearchBtn:'Search',memoryLtmPrev:'Prev',memoryLtmNext:'Next',
      memoryPreviewTitle:'Memory Detail',memoryLtmEmpty:'No long-term memories',memoryLtmSelectHint:'Select a memory to view details.',memoryLtmDelete:'Delete',
      memoryConfirmDelete:'Delete this long-term memory?'
    });
    Object.assign(i18n.zh,{
      you:'你',tool:'工具',streaming:'流式中',complete:'完成',tagChatName:'聊天',tagScheduleName:'定时',tagSettingsName:'设置',tagSkillsName:'技能',tagMemoryName:'记忆',settingsSideTitle:'设置',settingsTitle:'设置',skillsMainTitle:'技能编辑器',memoryMainTitle:'记忆',
      scheduleAgentLabel:'任务归属',schedulePhaseLabel:'低权重',scheduleQueueLabel:'队列',scheduleTraceLabel:'轨迹',scheduleTaskSpecLabel:'任务规格',scheduleContentPlaceholder:'写清楚任务目标、输入来源、输出要求...',scheduleRuleAdd:'添加',scheduleRuleInterval:'间隔分钟',
      scheduleActionEdit:'编辑',scheduleActionRead:'查看',scheduleActionTest:'测试',scheduleActionDelete:'删除',scheduleCreatedPrefix:'已创建',scheduleNone:'无',
      scheduleMoreSuffix:'更多',scheduleRunStatusSucceeded:'成功',scheduleRunStatusFailed:'失败',scheduleRunStatusRunning:'运行中',scheduleRunStatusUnknown:'未知',
      skillsReload:'刷新',skillsOrganize:'整理',skillsPhaseLabel:'可编辑',skillsEditorLabel:'技能编辑',skillsLibraryLabel:'技能库',skillsPreviewLabel:'预览',skillsNamePlaceholder:'技能名称（如：React组件开发规范）',skillsTagsPlaceholder:'逗号分隔，如: frontend, react, best-practice',
      skillsDescPlaceholder:'一句话描述这个技能的用途和使用场景',skillsContentPlaceholder:'## When to Use\n\n这个技能适用的任务场景。\n\n## Preconditions\n\n需要先确认的环境、权限、输入和依赖。\n\n## Workflow\n\n1. 可复现步骤。\n2. 决策分支和检查点。\n\n## Tools and Parameters\n\n- tool_name: 关键参数、返回值格式、成功/失败信号。\n\n## Expected Outputs\n\n用户或后续 Agent 应该拿到什么结果。\n\n## Failure Handling\n\n常见错误、诊断顺序、停止条件。\n\n## Boundaries\n\n不适用场景、隐私/凭据/安全边界。',
      skillsActionLoad:'加载',skillsActionExport:'导出',skillsActionDelete:'删除',skillsActionValidate:'验证并修复',skillsStateLoading:'加载中...',skillsLoadFailedPrefix:'加载失败',skillsLoadedPrefix:'已加载',skillsReadFailedPrefix:'读取失败',
      skillsUpdated:'已更新',skillsCreated:'已创建',skillsExported:'技能已导出',skillsExportFailedPrefix:'技能导出失败',skillsDeleteConfirm:'确定要删除这个技能吗？此操作不可撤销。',skillsSelectFirst:'请先选择一个技能',skillsOrganizeTitle:'整理技能中...',skillsOrganizeDesc:'正在分析近期会话并提取可复用技能',
      skillsValidateTitle:'验证并修复技能中...',skillsValidateDesc:'子代理正在尝试复现该技能、生成报告，并在边界明确时返回可应用的 skill-local 修复。',skillsJobPrepare:'准备中...',skillsOrganizeChecking:'检查主 Agent 状态...',skillsOrganizeBusy:'主 Agent 正在回复，请等待...',skillsOrganizeStarted:'技能整理任务已启动',
      skillsOrganizeComplete:'技能整理完成',skillsValidateStarted:'技能验证任务已启动',skillsValidateComplete:'技能验证完成',skillsJobFailedPrefix:'技能任务失败',skillsJobPollFailedPrefix:'技能任务轮询失败',skillsJobStartFailedPrefix:'技能任务启动失败',skillsValidationReportTitle:'验证报告',markdownContentLabel:'content (Markdown)',
      memoryPhaseLabel:'记忆',memoryUserPlaceholder:'输入 user.md 内容...',memoryIdentityPlaceholder:'输入 identity.md 内容...',memoryHotPlaceholder:'输入热记忆内容...',memoryCorePlaceholder:'输入核心记忆内容...',
      memoryLtmReset:'重置',memoryDateLabel:'日期',memoryModifiedAtLabel:'修改时间',memoryOrganizeTitle:'整理记忆中...',memoryOrganizeDesc:'正在分析会话并整理记忆文件',
      memoryOrganizePrepare:'准备中...',memoryOrganizeChecking:'检查主 Agent 状态...',memoryOrganizeBusy:'主 Agent 正在回复，请等待...',memoryOrganizeStarted:'整理任务已启动',
      memoryOrganizeComplete:'整理完成',memoryOrganizeNoChanges:'没有待整理的新变化',memoryOrganizeFailedPrefix:'整理失败',memoryOrganizePollFailedPrefix:'轮询失败',memoryOrganizeStartFailedPrefix:'整理启动失败',unknownError:'未知错误',
      phaseThinking:'思考中',phaseIntegrating:'整合中',phaseContinuing:'继续中',phaseTooling:'调用工具',toolRequestSent:'工具请求已发送',
      toolStatusRunning:'运行中',toolStatusDone:'完成',toolStatusError:'错误',toolStatusSkipped:'已跳过',toolStatusCalled:'已调用',noOutput:'无输出',summaryTruncated:'摘要已截断',
      noticeTaskLabel:'任务',noticeStatusLabel:'状态',noticeRunIdLabel:'Run ID',noticeScheduledAtLabel:'计划时间',noticeCompletedAtLabel:'完成时间',noticeCatchUpLabel:'补偿原因',
      blankClosedTitle:'已关闭测试页',blankMinimizedTitle:'已最小化测试页',blankActionText:'这是窗口控制触发的测试空白页。点击返回可回到界面。'
    });
    Object.assign(i18n.en,{
      scheduleAgentLabel:'Task Owner',schedulePhaseLabel:'low priority',scheduleContentPlaceholder:'Describe the task goal, inputs, and output requirements...',scheduleRuleAdd:'Add',scheduleRuleInterval:'Interval minutes',
      scheduleActionEdit:'Edit',scheduleActionRead:'View',scheduleActionTest:'Test',scheduleActionDelete:'Delete',scheduleCreatedPrefix:'Created',scheduleNone:'none',
      scheduleMoreSuffix:'more',scheduleRunStatusSucceeded:'Succeeded',scheduleRunStatusFailed:'Failed',scheduleRunStatusRunning:'Running',scheduleRunStatusUnknown:'Unknown',
      skillsReload:'Reload',skillsOrganize:'Organize',skillsPhaseLabel:'editable',skillsEditorLabel:'SKILL EDITOR',skillsLibraryLabel:'SKILL LIBRARY',skillsPreviewLabel:'PREVIEW',skillsNamePlaceholder:'Skill name, for example React component guidelines',skillsTagsPlaceholder:'Comma-separated, for example frontend, react, best-practice',
      skillsDescPlaceholder:'Describe what this skill is for and when to use it',skillsContentPlaceholder:'## When to Use\n\nTask situations where this skill applies.\n\n## Preconditions\n\nEnvironment, permissions, inputs, and dependencies to confirm first.\n\n## Workflow\n\n1. Reproducible steps.\n2. Decision branches and checkpoints.\n\n## Tools and Parameters\n\n- tool_name: important parameters, output shape, success/failure signals.\n\n## Expected Outputs\n\nWhat the user or a future agent should receive.\n\n## Failure Handling\n\nCommon errors, diagnostic order, and stop conditions.\n\n## Boundaries\n\nWhen not to use this skill; privacy, credential, and safety limits.',
      skillsActionLoad:'Load',skillsActionExport:'Export',skillsActionDelete:'Delete',skillsActionValidate:'Validate + Repair',skillsStateLoading:'Loading...',skillsLoadFailedPrefix:'Load failed',skillsLoadedPrefix:'Loaded',skillsReadFailedPrefix:'Read failed',
      skillsUpdated:'Updated',skillsCreated:'Created',skillsExported:'Skill exported',skillsExportFailedPrefix:'Skill export failed',skillsDeleteConfirm:'Delete this skill? This cannot be undone.',skillsSelectFirst:'Select a skill first',skillsOrganizeTitle:'Organizing skills...',skillsOrganizeDesc:'Analyzing recent sessions and extracting reusable skills',
      skillsValidateTitle:'Validating and repairing skill...',skillsValidateDesc:'A subagent is trying to reproduce the skill, generate a report, and return safe skill-local repairs when the boundary is clear.',skillsJobPrepare:'Preparing...',skillsOrganizeChecking:'Checking main Agent status...',skillsOrganizeBusy:'Main Agent is replying. Please wait...',skillsOrganizeStarted:'Skill organization task started',
      skillsOrganizeComplete:'Skill organization complete',skillsValidateStarted:'Skill validation task started',skillsValidateComplete:'Skill validation complete',skillsJobFailedPrefix:'Skill job failed',skillsJobPollFailedPrefix:'Skill job polling failed',skillsJobStartFailedPrefix:'Skill job start failed',skillsValidationReportTitle:'Validation Report',markdownContentLabel:'content (Markdown)',
      memoryPhaseLabel:'memory',memoryUserPlaceholder:'Enter user.md content...',memoryIdentityPlaceholder:'Enter identity.md content...',memoryHotPlaceholder:'Enter hot memory content...',memoryCorePlaceholder:'Enter core memory content...',
      memoryLtmReset:'Reset',memoryDateLabel:'Date',memoryModifiedAtLabel:'Modified at',memoryOrganizeTitle:'Organizing memory...',memoryOrganizeDesc:'Analyzing sessions and organizing memory files',
      memoryOrganizePrepare:'Preparing...',memoryOrganizeChecking:'Checking main Agent status...',memoryOrganizeBusy:'Main Agent is replying. Please wait...',memoryOrganizeStarted:'Organization task started',
      memoryOrganizeComplete:'Organization complete',memoryOrganizeNoChanges:'No pending changes to organize',memoryOrganizeFailedPrefix:'Organization failed',memoryOrganizePollFailedPrefix:'Polling failed',memoryOrganizeStartFailedPrefix:'Organization start failed',unknownError:'Unknown error',
      phaseThinking:'thinking',phaseIntegrating:'integrating',phaseContinuing:'continuing',phaseTooling:'using tools',toolRequestSent:'tool request sent',
      toolStatusRunning:'running',toolStatusDone:'done',toolStatusError:'error',toolStatusSkipped:'skipped',toolStatusCalled:'called',noOutput:'no output',summaryTruncated:'summary truncated',
      noticeTaskLabel:'Task',noticeStatusLabel:'Status',noticeRunIdLabel:'Run ID',noticeScheduledAtLabel:'Scheduled at',noticeCompletedAtLabel:'Completed at',noticeCatchUpLabel:'Catch-up reason',
      blankClosedTitle:'Closed Test Page',blankMinimizedTitle:'Minimized Test Page',blankActionText:'This is a test blank page triggered by the window control. Use Back to return.'
    });
    Object.assign(i18n.zh,{
      imageNoticeMeta:'图像生成',imageNoticeBadge:'图像任务',imageNoticeTitle:'图像生成宿主通知',
      imageNoticeJobId:'任务 ID',imageNoticeBatchId:'批次 ID',imageNoticeStatus:'状态',imageNoticeBatchStatus:'批次状态',
      imageNoticePrompt:'提示词',imageNoticeRequestedProfile:'请求 profile',imageNoticeFallback:'Provider 回退',imageNoticeProvider:'最终 provider/model',
      imageNoticeError:'错误',imageNoticeErrorCategory:'错误分类',imageNoticeFiles:'生成文件',imageNoticeBatchFiles:'批次生成文件',
      imageNoticeAuthority:'这是图像任务的宿主权威状态。',imageStatusQueued:'排队中',imageStatusRunning:'运行中',imageStatusSucceeded:'成功',
      imageStatusFailed:'失败',imageStatusCanceled:'已取消',imageStatusComplete:'已完成',imageStatusActive:'进行中',
      imageBatchSucceeded:'成功',imageBatchFailed:'失败',imageBatchCanceled:'取消',yes:'是'
    });
    Object.assign(i18n.en,{
      imageNoticeMeta:'Image generation',imageNoticeBadge:'Image job',imageNoticeTitle:'Image Generation Host Notice',
      imageNoticeJobId:'Job ID',imageNoticeBatchId:'Batch ID',imageNoticeStatus:'Status',imageNoticeBatchStatus:'Batch status',
      imageNoticePrompt:'Prompt',imageNoticeRequestedProfile:'Requested profile',imageNoticeFallback:'Provider fallback',imageNoticeProvider:'Final provider/model',
      imageNoticeError:'Error',imageNoticeErrorCategory:'Error category',imageNoticeFiles:'Generated files',imageNoticeBatchFiles:'Batch generated files',
      imageNoticeAuthority:'This is authoritative host state for the image job.',imageStatusQueued:'Queued',imageStatusRunning:'Running',imageStatusSucceeded:'Succeeded',
      imageStatusFailed:'Failed',imageStatusCanceled:'Canceled',imageStatusComplete:'Complete',imageStatusActive:'Active',
      imageBatchSucceeded:'succeeded',imageBatchFailed:'failed',imageBatchCanceled:'canceled',yes:'yes'
    });
    Object.assign(i18n.zh,{
      scheduleActionRetry:'\u91cd\u8bd5',scheduleActionRepairRetry:'\u4fee\u590d\u5e76\u91cd\u8bd5',
      scheduleSystemMemoryTitle:'\u7cfb\u7edf\u8bb0\u5fc6\u6574\u7406',scheduleSystemMemoryContent:'\u81ea\u52a8\u6574\u7406 agent \u7684\u8bb0\u5fc6\u6587\u4ef6\uff0c\u5305\u62ec hot_memory\u3001core_memory\u3001user.md\u3001identity.md \u548c\u957f\u671f\u8bb0\u5fc6\u5f52\u6863\u3002',
      scheduleSystemSkillTitle:'\u7cfb\u7edf\u6280\u80fd\u6574\u7406',scheduleSystemSkillContent:'\u81ea\u52a8\u5206\u6790\u8fd1\u671f\u4f1a\u8bdd\u5e76\u63d0\u53d6\u3001\u66f4\u65b0\u53ef\u590d\u7528\u6280\u80fd\uff0c\u4fdd\u6301\u6280\u80fd\u5e93\u6301\u7eed\u5b66\u4e60\u3002',
      scheduleConfirmRetry:'\u7acb\u5373\u5c06\u6b64\u4efb\u52a1\u91cd\u65b0\u52a0\u5165\u961f\u5217\uff1f',
      scheduleConfirmRepairRetry:'\u5c1d\u8bd5\u4fee\u590d\u4efb\u52a1\u7ed3\u6784\u5e76\u63d0\u4ea4\u4e00\u4efd\u62a2\u4fee\u7248\u91cd\u8bd5\uff1f',
      scheduleStalledUntil:'\u9000\u907f\u5230',scheduleHeartbeat:'\u6700\u8fd1\u5fc3\u8df3',
      scheduleRunStatusCanceled:'\u5df2\u53d6\u6d88',scheduleRunStatusInterrupted:'\u5df2\u4e2d\u65ad',scheduleRunStatusStalled:'\u505c\u6ede',
      scheduleDiagnostic:'\u8bca\u65ad'
    });
    Object.assign(i18n.en,{
      scheduleActionRetry:'Retry',scheduleActionRepairRetry:'Repair + Retry',
      scheduleSystemMemoryTitle:'System Memory Organization',scheduleSystemMemoryContent:"Automatically organizes this agent's memory files, including hot_memory, core_memory, user.md, identity.md, and long-term memory archives.",
      scheduleSystemSkillTitle:'System Skill Organization',scheduleSystemSkillContent:'Automatically analyzes recent conversations, extracts or updates reusable skills, and keeps the skill library learning over time.',
      scheduleConfirmRetry:'Queue this task for an immediate retry?',
      scheduleConfirmRepairRetry:'Repair the task structure, replace the damaged active item, and retry?',
      scheduleStalledUntil:'Backoff until',scheduleHeartbeat:'Last heartbeat',
      scheduleRunStatusCanceled:'Canceled',scheduleRunStatusInterrupted:'Interrupted',scheduleRunStatusStalled:'Stalled',
      scheduleDiagnostic:'Diagnostic'
    });
    Object.assign(i18n.zh,{
      memoryTabVector:'\u5411\u91cf\u8bb0\u5fc6',memoryVectorResultsTitle:'\u5411\u91cf\u641c\u7d22',memoryVectorAtlasTitle:'2D \u795e\u7ecf\u5143\u56fe\u518c',
      memoryVectorSearch:'\u641c\u7d22',memoryVectorSearchPlaceholder:'\u641c\u7d22\u6280\u80fd\u3001\u4e8b\u5b9e\u3001\u504f\u597d\u6216\u65e5\u671f...',
      memoryVectorEmpty:'\u8f93\u5165\u67e5\u8be2\u4ee5\u641c\u7d22\u5411\u91cf\u8bb0\u5fc6\u3002',memoryVectorNoResults:'\u6ca1\u6709\u627e\u5230\u76f8\u5173\u5411\u91cf\u8bb0\u5fc6\u3002',
      memoryVectorLoading:'\u6b63\u5728\u8bfb\u53d6\u5411\u91cf\u7d22\u5f15...',memoryVectorSearchLoading:'\u6b63\u5728\u68c0\u7d22...',
      memoryVectorAtlasEmpty:'\u5411\u91cf\u7d22\u5f15\u4e3a\u7a7a\uff0c\u5148\u4fdd\u5b58\u6216\u6574\u7406\u8bb0\u5fc6\u3002',
      memoryVectorNodes:'\u8282\u70b9',memoryVectorEntries:'\u6761\u8bb0\u5fc6',memoryVectorCandidates:'\u5019\u9009',memoryVectorVisited:'\u8bbf\u95ee',memoryVectorUpdated:'\u66f4\u65b0',
      memoryVectorScore:'\u5206\u6570',memoryVectorLine:'\u884c',memoryVectorHint:'\u60ac\u505c\u8282\u70b9\u67e5\u770b\u6458\u8981\uff0c\u70b9\u51fb\u8282\u70b9\u56fa\u5b9a\u9ad8\u4eae\u3002'
    });
    Object.assign(i18n.en,{
      memoryTabVector:'Vector Memory',memoryVectorResultsTitle:'Vector Search',memoryVectorAtlasTitle:'2D Neuron Atlas',
      memoryVectorSearch:'Search',memoryVectorSearchPlaceholder:'Search skills, facts, preferences, or dates...',
      memoryVectorEmpty:'Enter a query to search vector memory.',memoryVectorNoResults:'No relevant vector memories found.',
      memoryVectorLoading:'Reading vector index...',memoryVectorSearchLoading:'Searching...',
      memoryVectorAtlasEmpty:'Vector index is empty. Save or organize memory first.',
      memoryVectorNodes:'nodes',memoryVectorEntries:'entries',memoryVectorCandidates:'candidates',memoryVectorVisited:'visited',memoryVectorUpdated:'updated',
      memoryVectorScore:'score',memoryVectorLine:'line',memoryVectorHint:'Hover nodes for snippets; click a node to pin highlight.'
    });
    Object.assign(i18n.zh,{
      tagLabName:'调试星球',tagLabDescription:'测试 image_generation、text_to_speech、speech_to_text 和内置组件，确认配置与真实运行结果一致。',
      cmdLabDesc:'切换到调试星球',cmdLabDone:'已切换到调试星球。',
      apiTypeHelp:'openai_chat 走 chat completions；anthropic 表示 Messages 兼容端点，支持工具调用，Base URL、模型 ID、上下文和输出上限都可按兼容提供商填写。',
      multiTitle:'多模态端点',multiDescription:'配置图像生成、语音合成和浏览器语音识别。Key 只写不回显，留空保留原值。',
      multiGlobalTitle:'全局配置',multiSave:'保存多模态',
      multiImageProfiles:'图像生成模型',multiTextToSpeech:'语音合成',multiSpeechToText:'语音转文字',multiAdd:'添加',
      multiRemove:'删除',multiFieldEnabled:'启用',multiFieldProfileId:'配置 ID',multiFieldDisplayName:'显示名',
      multiFieldEndpoint:'端点',multiFieldBaseUrl:'Base URL',multiFieldApiKey:'API Key',multiFieldModel:'模型',
      multiFieldSize:'尺寸',multiFieldQuality:'质量',multiFieldFormat:'格式',multiFieldMode:'模式',
      multiFieldAutoPlay:'自动播放',multiFieldVoice:'音色',multiFieldLanguage:'语种',multiFieldInstructions:'风格指令',
      multiFieldOptimizeInstructions:'优化指令',multiFieldSendAfter:'识别后发送',
      multiMetaBool:'布尔',multiMetaMode:'模式',multiMetaValue:'值',multiMetaWriteOnly:'只写',
      multiOptionInherit:'继承/默认',multiOptionEnabled:'启用',multiOptionDisabled:'禁用',
      multiOptionNativeImage:'v1/images/generations',multiOptionNativeSpeech:'v1/audio/speech',
      multiOptionTts:'v1/tts',multiOptionAliyunQwenTts:'阿里云千问 TTS',multiOptionChatCompletions:'v1/chat/completions',
      multiOptionOff:'关闭',multiOptionChatVisible:'聊天可见时',multiOptionAlways:'始终',
      multiApiKeyConfigured:'已配置；留空保留',multiApiKeyEmpty:'粘贴 key，留空表示不配置',
      multiAliyunBaseNote:'阿里云千问 TTS 会默认使用 DashScope 地址，不需要填写 Base URL。',
      multiSttBrowserNote:'使用浏览器 Microsoft/Web Speech 在线识别路径；这里不再配置云端 STT API key。',
      voiceInput:'语音输入',voiceHold:'长按录制',voiceHoldHint:'松开发送，上滑取消',
      voiceReleaseSend:'松开发送，上滑取消',voiceSlideCancel:'松手取消',voiceTranscribing:'正在转换...',
      voiceCanceled:'已取消录音',voiceNoSpeech:'没有识别到语音。',
      labHeroTitle:'调试星球',labHeroText:'直接测试真实工具和内置组件，用运行结果确认配置是否可用。',
      labAgentLabel:'测试目标',labNoteTitle:'运行时状态',labNoteText:'这里调用的是 agent 和聊天页同一套端点。Lab 里的结果可以直接反映当前配置是否可用。',
      labMainTitle:'Lab',labPhaseLabel:'debug',labState:'选择 agent 后测试已配置的多模态端点。',
      labImageTitle:'图像生成',labTtsTitle:'语音合成',labSttTitle:'语音转文字',
      labImageRun:'生成',labTtsRun:'合成',labSttRun:'转写文件',labSttRecord:'录音',
      labImagePlaceholder:'描述要生成的图像...',labTtsPlaceholder:'输入要朗读的文本...',labTtsVoicePlaceholder:'voice 覆盖，可留空',
      labNoImageReturned:'没有返回图像。',
      labGeneratingSpeech:'正在生成语音...',
      labPlayAudio:'播放',
      labFileSttUnavailable:'文件转写当前不可用；请使用录音识别。',
      labRecording:'录音中...',
      labNoTranscript:'没有返回转写文本。',
      labBrowserSpeechReady:'浏览器语音识别就绪',
      labBrowserSpeechUnavailable:'浏览器语音识别不可用'
    });
    Object.assign(i18n.en,{
      tagLabName:'Debug Lab',tagLabDescription:'Test image_generation, text_to_speech, speech_to_text, and built-in components against real runtime results.',
      cmdLabDesc:'Switch to Debug Lab',cmdLabDone:'Switched to Debug Lab.',
      apiTypeHelp:'openai_chat uses chat completions; anthropic means a Messages-compatible endpoint with tool support. Base URL, model ID, context, and output limits can follow the compatible provider.',
      multiTitle:'Multimodal Endpoints',multiDescription:'Configure image generation, speech synthesis, and browser speech recognition. Keys are write-only; blank keeps the existing value.',
      multiGlobalTitle:'Global config',multiSave:'Save Multimodal',
      multiImageProfiles:'Image generation models',multiTtsProfiles:'TTS voice models',multiSearchProfiles:'Web search providers',multiTextToSpeech:'Text to speech',multiSpeechToText:'Speech to text',multiAdd:'Add',
      multiRemove:'Remove',multiFieldEnabled:'Enabled',multiFieldProfileId:'Profile ID',multiFieldDisplayName:'Display Name',
      multiFieldEndpoint:'Endpoint',multiFieldBaseUrl:'Base URL',multiFieldApiKey:'API Key',multiFieldModel:'Model',
      multiFieldSize:'Size',multiFieldQuality:'Quality',multiFieldFormat:'Format',multiFieldMode:'Mode',
      multiFieldAutoPlay:'Auto play',multiFieldVoice:'Voice',multiFieldLanguage:'Language',multiFieldInstructions:'Instructions',
      multiFieldOptimizeInstructions:'Optimize instructions',multiFieldSendAfter:'Send after transcription',multiFieldProvider:'Provider',multiFieldEndpointPath:'Endpoint path',multiFieldMaxResults:'Max results',
      multiMetaBool:'bool',multiMetaMode:'mode',multiMetaValue:'value',multiMetaWriteOnly:'write-only',
      multiOptionInherit:'inherit/default',multiOptionEnabled:'enabled',multiOptionDisabled:'disabled',
      multiOptionNativeImage:'v1/images/generations',multiOptionNativeSpeech:'v1/audio/speech',
      multiOptionTts:'v1/tts',multiOptionAliyunQwenTts:'Aliyun Qwen TTS',multiOptionChatCompletions:'v1/chat/completions',
      multiOptionOff:'off',multiOptionChatVisible:'chat visible only',multiOptionAlways:'always',
      multiOptionSearchTavily:'Tavily',multiOptionSearchBrave:'Brave Search',multiOptionSearchFirecrawl:'Firecrawl',multiOptionSearchCustom:'Custom',
      multiApiKeyConfigured:'Configured; leave blank to keep it',multiApiKeyEmpty:'Paste key, blank keeps empty',
      multiAliyunBaseNote:'Aliyun Qwen TTS uses the DashScope base URL by default, so no Base URL is needed.',
      multiSttBrowserNote:'Uses the browser Microsoft/Web Speech online recognition path. No cloud STT API key is configured here.',
      voiceInput:'Voice input',voiceHold:'Hold to record',voiceHoldHint:'Release to send, slide up to cancel',
      voiceReleaseSend:'Release to send, slide up to cancel',voiceSlideCancel:'Release to cancel',voiceTranscribing:'Transcribing...',
      voiceCanceled:'Recording canceled',voiceNoSpeech:'No speech recognized.',
      labHeroTitle:'Debug Lab',labHeroText:'Test real tools and built-in components, then judge the configuration from runtime results.',
      labAgentLabel:'test target',labNoteTitle:'Runtime state',labNoteText:'These buttons call the same endpoints the agent and chat UI use. Lab results directly reflect whether the current configuration works.',
      labMainTitle:'Lab',labPhaseLabel:'debug',labState:'Select an agent and test configured multimodal endpoints.',
      labImageTitle:'Image Generation',labTtsTitle:'Text To Speech',labSttTitle:'Speech To Text',
      labImageRun:'Generate',labTtsRun:'Speak',labSttRun:'Transcribe File',labSttRecord:'Record',
      labImagePlaceholder:'Describe the image to generate...',labTtsPlaceholder:'Text to speak...',labTtsVoicePlaceholder:'voice override, optional',
      labNoImageReturned:'No image returned.',
      labGeneratingSpeech:'Generating speech...',
      labPlayAudio:'Play',
      labFileSttUnavailable:'File transcription is not available here; use recording instead.',
      labRecording:'Recording...',
      labNoTranscript:'No transcript returned.',
      labBrowserSpeechReady:'browser speech ready',
      labBrowserSpeechUnavailable:'browser speech unavailable'
    });
    Object.assign(i18n.zh,{apiTypeHelp:'openai_chat 走 chat completions；DeepSeek / GLM / MiMo / MiMo Token Plan / 百度千帆 Coding Plan 会自动补全并锁定 Base URL、模型和额度预设；anthropic 表示 Messages 兼容端点，Base URL、模型 ID、上下文和输出上限都可自定义。',apiKeyLink:'从此处获取 API Key',modelDropdownLabel:'模型列表',modelDropdownEmpty:'没有匹配的模型 ID'});
    Object.assign(i18n.en,{apiTypeHelp:'openai_chat uses chat completions. DeepSeek / GLM / MiMo / MiMo Token Plan / Baidu Qianfan Coding Plan auto-fill and lock provider presets. anthropic means a Messages-compatible endpoint; Base URL, model ID, context, and output limits are editable.',apiKeyLink:'Get API Key here',modelDropdownLabel:'Model list',modelDropdownEmpty:'No matching model IDs'});
    Object.assign(i18n.zh,{
      cancel:'\u53d6\u6d88',start:'\u5f00\u59cb',skillsLearnValidate:'\u5b66\u4e60\u5e76\u9a8c\u8bc1',skillsLearnTitle:'\u5b66\u4e60\u5e76\u9a8c\u8bc1\u5916\u90e8\u6280\u80fd',skillsLearnDesc:'\u9009\u62e9\u6587\u4ef6\u3001\u6587\u4ef6\u5939\u3001zip \u538b\u7f29\u5305\uff0c\u6216\u7c98\u8d34\u7eaf\u6587\u672c\u3002\u5916\u90e8\u5185\u5bb9\u4f1a\u88ab\u5f53\u4f5c\u4e0d\u53ef\u4fe1\u8f93\u5165\u672c\u5730\u5316\u3002',
      skillsLearnNameHintLabel:'\u540d\u79f0\u63d0\u793a',skillsLearnPathLabel:'\u672c\u5730\u8def\u5f84',skillsLearnFileLabel:'\u6587\u4ef6\u6765\u6e90',skillsLearnTextLabel:'\u5916\u90e8\u6750\u6599',skillsLearnNameHintMeta:'\u53ef\u9009',skillsLearnPathMeta:'\u6587\u4ef6\u6216\u76ee\u5f55',skillsLearnFileMeta:'\u6587\u4ef6\u5939 / zip / txt / md',skillsLearnTextMeta:'\u4e0d\u53ef\u4fe1',skillsLearnNameHintPlaceholder:'\u53ef\u9009\uff0c\u4f8b\u5982\uff1a\u8bb0\u5fc6\u7ba1\u7406\u65b9\u6cd5',skillsLearnPathPlaceholder:'\u53ef\u9009\uff0c\u672c\u5730 skill \u6587\u4ef6\u3001zip \u6216\u76ee\u5f55\u8def\u5f84',skillsLearnTextPlaceholder:'\u53ef\u9009\uff0c\u7c98\u8d34\u5916\u90e8 skill/README/\u8bf4\u660e\u6587\u672c',
      skillsLearnChooseFiles:'\u9009\u62e9\u6587\u4ef6/\u538b\u7f29\u5305',skillsLearnChooseFolder:'\u9009\u62e9\u6587\u4ef6\u5939',skillsLearnClearFiles:'\u6e05\u9664',skillsLearnNoFiles:'\u5c1a\u672a\u9009\u62e9\u6587\u4ef6\u3002',skillsLearnSelectedFiles:'\u5df2\u9009 {count} \u4e2a\u6587\u4ef6\uff0c\u5171 {size}',skillsLearnMoreFiles:'\u7b49 {count} \u4e2a',skillsLearnClose:'\u5173\u95ed',
      skillsLearnNeedSource:'\u8bf7\u9009\u62e9\u6587\u4ef6/\u6587\u4ef6\u5939\u3001\u586b\u5199\u672c\u5730\u8def\u5f84\uff0c\u6216\u7c98\u8d34\u5916\u90e8\u6750\u6599\u3002',skillsLearnStarted:'\u5b66\u4e60\u5e76\u9a8c\u8bc1\u4efb\u52a1\u5df2\u542f\u52a8',skillsLearnTitleProgress:'\u5b66\u4e60\u5e76\u9a8c\u8bc1\u4e2d...',skillsLearnDescProgress:'\u5b66\u4e60 subagent \u6b63\u5728\u5c06\u5916\u90e8\u6750\u6599\u5b89\u5168\u672c\u5730\u5316\uff0c\u7136\u540e\u8c03\u7528\u9a8c\u8bc1\u6d41\u7a0b\u3002',skillsLearnComplete:'\u5b66\u4e60\u5e76\u9a8c\u8bc1\u5b8c\u6210',
      skillsContentPreviewTitle:'\u6280\u80fd\u5185\u5bb9',skillsNoValidationReport:'\u5c1a\u65e0\u9a8c\u8bc1\u62a5\u544a\uff0c\u6682\u65f6\u663e\u793a\u6280\u80fd\u5185\u5bb9\u3002'
    });
    Object.assign(i18n.en,{
      cancel:'Cancel',start:'Start',skillsLearnValidate:'Learn + Validate',skillsLearnTitle:'Learn and validate external skill',skillsLearnDesc:'Choose files, a folder, a zip package, or paste plain text. External material is treated as untrusted input and localized before validation.',
      skillsLearnNameHintLabel:'Name hint',skillsLearnPathLabel:'Local path',skillsLearnFileLabel:'File source',skillsLearnTextLabel:'External material',skillsLearnNameHintMeta:'optional',skillsLearnPathMeta:'file or folder',skillsLearnFileMeta:'folder / zip / txt / md',skillsLearnTextMeta:'untrusted',skillsLearnNameHintPlaceholder:'Optional, for example memory management method',skillsLearnPathPlaceholder:'Optional local skill file, zip, or directory path',skillsLearnTextPlaceholder:'Optional pasted external skill, README, or notes',
      skillsLearnChooseFiles:'Choose files/package',skillsLearnChooseFolder:'Choose folder',skillsLearnClearFiles:'Clear',skillsLearnNoFiles:'No files selected.',skillsLearnSelectedFiles:'{count} files selected, {size} total',skillsLearnMoreFiles:'and {count} more',skillsLearnClose:'Close',
      skillsLearnNeedSource:'Choose files/a folder, provide a local path, or paste external material.',skillsLearnStarted:'Learning and validation task started',skillsLearnTitleProgress:'Learning and validating skill...',skillsLearnDescProgress:'A learning subagent is safely localizing external material, then calling validation.',skillsLearnComplete:'Learning and validation complete',
      skillsContentPreviewTitle:'Skill Content',skillsNoValidationReport:'No validation report yet, showing skill content.'
    });
    Object.assign(i18n.zh,{
      settingsNavGeneralTitle:'\u901a\u7528',settingsNavGeneralSub:'\u8bed\u8a00\u548c\u754c\u9762',
      settingsNavMemoryTitle:'\u8bb0\u5fc6',settingsNavMemorySub:'\u4e0a\u4e0b\u6587\u4e0a\u9650',
      settingsNavSoundTitle:'\u63d0\u793a\u97f3',settingsNavSoundSub:'Agent \u72b6\u6001\u4e0e\u58f0\u97f3\u8d44\u4ea7',
      settingsNavMultiTitle:'\u591a\u6a21\u6001',settingsNavMultiSub:'\u56fe\u50cf\u548c\u8bed\u97f3\u6a21\u578b',
      settingsGeneralTitle:'\u754c\u9762\u504f\u597d',settingsGeneralDesc:'\u4fdd\u5b58\u5728\u5f53\u524d\u6d4f\u89c8\u5668\u7684\u672c\u5730\u9009\u9879\uff0c\u907f\u514d\u548c\u6a21\u578b\u7aef\u70b9\u914d\u7f6e\u6324\u5728\u4e00\u8d77\u3002',settingsGeneralState:'\u4ec5\u672c\u5730',
      runtimeEventsTitle:'后台事件',runtimeEventsDesc:'当前 Agent 最近的 subagent、调度和恢复事件。',runtimeEventsReload:'刷新事件',runtimeEventsEmpty:'暂无后台事件',runtimeAdvice_no_action_needed:'无需操作。',runtimeAdvice_wait_for_completion:'等待任务完成。',runtimeAdvice_retry_manual:'可手动重试；如果连续失败，请检查 API Key、网络和任务输入。',runtimeAdvice_review_memory:'请检查记忆文件，必要时从快照回滚。',runtimeAdvice_review_skills:'请检查技能内容和资源文件，必要时重新验证。',runtimeAdvice_review_validation_report:'请查看验证报告并按建议修复。',runtimeAdvice_review_result:'请查看任务结果。',runtimeAdvice_wait_for_idle:'等待当前用户回合结束后会自动重试。',runtimeAdvice_review_import_source:'请检查导入材料是否足够具体、可复用。',
      settingsReserveTitle:'\u8bbe\u7f6e\u72b6\u6001',settingsReserveLanguage:'\u8bed\u8a00',settingsReserveTheme:'\u4e3b\u9898',settingsReserveShortcuts:'\u5feb\u6377\u952e',settingsReserveAbout:'\u5173\u4e8e',
      soundCueTitle:'Agent \u63d0\u793a\u97f3',soundCueDesc:'\u77ed\u4fc3\u3001\u975e\u4eba\u58f0\u7684\u7cfb\u7edf\u97f3\uff0c\u7528\u4e8e thinking\u3001\u56de\u590d\u5b8c\u6210\u548c {play_audio:type} \u6807\u8bb0\u3002',
      soundCueEnabledLabel:'\u542f\u7528\u63d0\u793a\u97f3',soundCueVolumeLabel:'\u97f3\u91cf',soundCueDelayLabel:'\u72b6\u6001\u5361\u7247\u95f4\u9694',soundCueLibraryTitle:'\u58f0\u97f3\u5217\u8868',
      soundCueLibraryDesc:'\u6bcf\u79cd\u7c7b\u578b\u90fd\u6709\u4e00\u4e2a\u5217\u8868\uff0c\u89e6\u53d1\u65f6\u968f\u673a\u62bd\u53d6\u5176\u4e2d\u4e00\u4e2a\u64ad\u653e\u3002',
      soundCueEventThinking:'thinking',soundCueEventReply:'reply',soundCueEventDelay:'\u5f53\u524d\u63d0\u793a\u97f3\u5f00\u59cb\u64ad\u653e\u540e\uff0c\u6309\u8bbe\u7f6e\u505c\u987f\u518d\u5c55\u793a\u540e\u7eed\u5185\u5bb9',soundCueEventWaiting:'\u505c\u987f\u4e2d',soundCueEventSaved:'\u72b6\u6001\u5df2\u4fdd\u7559',
      soundCuePreview:'\u9884\u89c8',soundCueUpload:'\u4e0a\u4f20',soundCueRemove:'\u79fb\u9664',soundCueDefault:'\u9ed8\u8ba4',soundCueCustom:'\u81ea\u5b9a\u4e49',
      soundCueUploadNoAgent:'\u8bf7\u5148\u9009\u62e9 agent \u518d\u4e0a\u4f20\u63d0\u793a\u97f3\u3002',soundCueUploadFailedPrefix:'\u63d0\u793a\u97f3\u4e0a\u4f20\u5931\u8d25',soundCueAssetToggle:'\u542f\u7528\u6216\u7981\u7528\u8fd9\u4e2a\u97f3\u9891',soundCueAssetEnabled:'\u542f\u7528',soundCueAssetDisabled:'\u7981\u7528',soundCueNeedOneEnabled:'\u8be5\u63d0\u793a\u97f3\u7c7b\u578b\u5df2\u542f\u7528\uff0c\u81f3\u5c11\u9700\u8981\u4fdd\u7559\u4e00\u4e2a\u53ef\u64ad\u653e\u97f3\u9891\u3002',soundCueTypeCount:'\u7c7b',
      soundCueGroupFlowTitle:'\u57fa\u7840\u6d41\u7a0b',soundCueGroupFlowDesc:'\u601d\u8003\u3001\u63a8\u8fdb\u3001\u7075\u611f\u3001\u7ed3\u675f\u7b49\u9ad8\u9891\u72b6\u6001\u3002',
      soundCueGroupPositiveTitle:'\u79ef\u6781\u9ad8\u80fd',soundCueGroupPositiveDesc:'\u81ea\u4fe1\u3001\u5f00\u5fc3\u3001\u5e86\u795d\u548c\u6e29\u67d4\u7684\u6b63\u5411\u53cd\u5e94\u3002',
      soundCueGroupUncertainTitle:'\u4e0d\u786e\u5b9a\u4e0e\u6c42\u52a9',soundCueGroupUncertainDesc:'\u56f0\u60d1\u3001\u6000\u7591\u3001\u5c34\u5c2c\u3001\u9053\u6b49\u548c\u5bfb\u6c42\u5e2e\u52a9\u3002',
      soundCueGroupLowTitle:'\u4f4e\u80fd\u53d7\u632b',soundCueGroupLowDesc:'\u96be\u8fc7\u3001\u75b2\u60eb\u3001\u4fe1\u5fc3\u4e0d\u8db3\u7684\u6536\u655b\u60c5\u7eea\u3002',
      soundCueGroupStrongTitle:'\u5f3a\u70c8\u6001\u5ea6',soundCueGroupStrongDesc:'\u6124\u6012\u6216\u6577\u884d\u7b49\u9700\u8981\u660e\u786e\u8868\u8fbe\u7684\u72b6\u6001\u3002',
      soundCueGroupCustomTitle:'\u81ea\u5b9a\u4e49',soundCueGroupCustomDesc:'\u81ea\u5b9a\u4e49\u60c5\u7eea\u7c7b\u578b\uff0c\u4f8b\u5982\u5f00\u6000\u5927\u7b11\u3001\u9119\u5937\u3001\u60a0\u95f2\u3002',
      soundCueCustomNamePlaceholder:'\u60c5\u7eea\u7c7b\u578b\uff0c\u4f8b\u5982\uff1a\u5f00\u6000\u5927\u7b11',soundCueCustomDescPlaceholder:'\u53ef\u9009\u8bf4\u660e\uff0c\u4f8b\u5982\uff1a\u8f7b\u677e\u5927\u7b11\u7684\u53cd\u5e94',soundCueCustomAdd:'\u65b0\u589e\u7c7b\u578b',soundCueCustomSave:'\u4fdd\u5b58',soundCueCustomDelete:'\u5220\u9664\u7c7b\u578b',soundCueCustomDeleteConfirm:'\u5220\u9664\u8fd9\u4e2a\u81ea\u5b9a\u4e49\u60c5\u7eea\u7c7b\u578b\u548c\u5176\u97f3\u9891\u5217\u8868\uff1f',soundCueCustomEmpty:'\u5c1a\u65e0\u81ea\u5b9a\u4e49\u60c5\u7eea\u7c7b\u578b\u3002\u5148\u65b0\u589e\u4e00\u4e2a\uff0c\u518d\u4e0a\u4f20\u97f3\u9891\u3002',
      soundCueImport:'\u5bfc\u5165',soundCueExport:'\u5bfc\u51fa',soundCueImportFailedPrefix:'\u63d0\u793a\u97f3\u914d\u7f6e\u5bfc\u5165\u5931\u8d25',soundCueExportFailedPrefix:'\u63d0\u793a\u97f3\u914d\u7f6e\u5bfc\u51fa\u5931\u8d25',soundCueImportDone:'\u63d0\u793a\u97f3\u914d\u7f6e\u5df2\u5bfc\u5165',soundCueExportDone:'\u63d0\u793a\u97f3\u914d\u7f6e\u5df2\u5bfc\u51fa',
      soundCueTypeReplyDoneTitle:'Agent \u56de\u590d\u5b8c\u6bd5',soundCueTypeReplyDoneDesc:'\u6b63\u5e38\u98ce\u683c\uff0c\u7528\u4e8e\u4e00\u8f6e\u56de\u590d\u7ed3\u675f\u3002',
      soundCueTypeThinkingTitle:'Thinking',soundCueTypeThinkingDesc:'\u610f\u5473\u6df1\u957f\u7684\u601d\u8003\u63d0\u793a\u97f3\u3002',
      soundCueTypeConfusedTitle:'Agent \u56f0\u60d1',soundCueTypeConfusedDesc:'\u660e\u663e\u56f0\u60d1\u3001\u5e26\u4e00\u70b9\u6447\u6446\u611f\u7684\u7cfb\u7edf\u97f3\u3002',
      soundCueTypeHelpTitle:'Agent \u5bfb\u6c42\u5e2e\u52a9',soundCueTypeHelpDesc:'\u6025\u4fc3\u4f46\u4e0d\u523a\u8033\uff0c\u5e26\u4e00\u70b9\u59d4\u5c48\u611f\u3002',
      soundCueTypeConfidentTitle:'Agent \u4fe1\u5fc3\u6ee1\u6ee1',soundCueTypeConfidentDesc:'\u6b22\u5feb\u6109\u60a6\uff0c\u9002\u5408\u8868\u793a\u5f88\u6709\u628a\u63e1\u3002',
      soundCueTypeLowConfidenceTitle:'Agent \u4fe1\u5fc3\u4e0d\u8db3',soundCueTypeLowConfidenceDesc:'\u4f4e\u6c89\u7684\u53d7\u632b\u611f\uff0c\u7528\u4e8e\u88ab\u6253\u51fb\u6216\u628a\u63e1\u4e0d\u8db3\u3002',
      soundCueTypeIdeaTitle:'Agent \u7a81\u7136\u60f3\u5230\u4ec0\u4e48',soundCueTypeIdeaDesc:'\u6709\u201c\u55ef~\u201d\u90a3\u79cd\u611f\u89c9\uff0c\u8f7b\u5feb\u7075\u52a8\u3002',
      multiDescription:'\u914d\u7f6e\u56fe\u50cf\u751f\u6210\u3001TTS \u8bed\u97f3\u8d44\u4ea7\u3001Web \u641c\u7d22\u548c\u6d4f\u89c8\u5668\u8bed\u97f3\u8bc6\u522b\u3002Key \u53ea\u5199\u4e0d\u56de\u663e\uff0c\u7559\u7a7a\u4fdd\u7559\u65e7\u503c\u3002',
      multiTtsProfiles:'TTS \u8bed\u97f3\u6a21\u578b',multiSearchProfiles:'Web \u641c\u7d22\u63d0\u4f9b\u5546',multiFieldProfileId:'Profile ID',multiFieldDisplayName:'\u663e\u793a\u540d',
      multiFieldProvider:'\u63d0\u4f9b\u5546',multiFieldEndpointPath:'Endpoint Path',multiFieldMaxResults:'\u7ed3\u679c\u6570',
      multiOptionSearchTavily:'Tavily',multiOptionSearchBrave:'Brave Search',multiOptionSearchFirecrawl:'Firecrawl',multiOptionSearchCustom:'\u81ea\u5b9a\u4e49'
    });
    Object.assign(i18n.en,{
      settingsNavGeneralTitle:'General',settingsNavGeneralSub:'Language and interface',
      settingsNavMemoryTitle:'Memory',settingsNavMemorySub:'Context limits',
      settingsNavSoundTitle:'Sound',settingsNavSoundSub:'Agent cues and assets',
      settingsNavMultiTitle:'Multimodal',settingsNavMultiSub:'Image and audio models',
      settingsGeneralTitle:'Interface Preferences',settingsGeneralDesc:'Local browser preferences live here so endpoint configuration stays focused.',settingsGeneralState:'Local only',
      runtimeEventsTitle:'Background Events',runtimeEventsDesc:'Recent subagent, scheduler, and recovery events for the selected agent.',runtimeEventsReload:'Reload Events',runtimeEventsEmpty:'No background events yet',runtimeAdvice_no_action_needed:'No action needed.',runtimeAdvice_wait_for_completion:'Wait for the task to finish.',runtimeAdvice_retry_manual:'Retry manually. If it keeps failing, check API keys, network, and task input.',runtimeAdvice_review_memory:'Review memory files; restore a snapshot if needed.',runtimeAdvice_review_skills:'Review skill content and resource files; revalidate if needed.',runtimeAdvice_review_validation_report:'Open the validation report and apply suggested repairs.',runtimeAdvice_review_result:'Review the task result.',runtimeAdvice_wait_for_idle:'It will retry after the current user turn finishes.',runtimeAdvice_review_import_source:'Check whether the imported material is concrete and reusable enough.',
      settingsReserveTitle:'Settings Status',settingsReserveLanguage:'Language',settingsReserveTheme:'Theme',settingsReserveShortcuts:'Shortcuts',settingsReserveAbout:'About',
      soundCueTitle:'Agent Sound Cues',soundCueDesc:'Short non-voice system sounds for thinking, reply completion, and {play_audio:type} markers.',
      soundCueEnabledLabel:'Enable sound cues',soundCueVolumeLabel:'Volume',soundCueDelayLabel:'Cue card delay',soundCueLibraryTitle:'Cue Library',
      soundCueLibraryDesc:'Each cue type has a list; Matdance randomly chooses one asset when the cue fires.',
      soundCueEventThinking:'thinking',soundCueEventReply:'reply',soundCueEventDelay:'After this cue starts playing, pause using the configured delay before later content appears',soundCueEventWaiting:'pausing',soundCueEventSaved:'saved',
      soundCuePreview:'Preview',soundCueUpload:'Upload',soundCueRemove:'Remove',soundCueDefault:'default',soundCueCustom:'custom',
      soundCueUploadNoAgent:'Select an agent before uploading a sound cue.',soundCueUploadFailedPrefix:'Sound cue upload failed',soundCueAssetToggle:'Enable or disable this audio asset',soundCueAssetEnabled:'Enabled',soundCueAssetDisabled:'Disabled',soundCueNeedOneEnabled:'This cue type is enabled, so at least one audio asset must remain enabled.',soundCueTypeCount:'types',
      soundCueGroupFlowTitle:'Flow',soundCueGroupFlowDesc:'High-frequency states such as thinking, effort, ideas, alerts, and completion.',
      soundCueGroupPositiveTitle:'Positive energy',soundCueGroupPositiveDesc:'Confident, happy, celebratory, playful, and gentle reactions.',
      soundCueGroupUncertainTitle:'Uncertain and help',soundCueGroupUncertainDesc:'Confusion, doubt, awkwardness, apology, surprise, and help-seeking.',
      soundCueGroupLowTitle:'Low energy',soundCueGroupLowDesc:'Sad, tired, and low-confidence states.',
      soundCueGroupStrongTitle:'Strong attitude',soundCueGroupStrongDesc:'Clear expressive states such as anger or perfunctory responses.',
      soundCueGroupCustomTitle:'Custom',soundCueGroupCustomDesc:'Custom emotion types, such as hearty laugh, contempt, or relaxed.',
      soundCueCustomNamePlaceholder:'Emotion type, e.g. hearty laugh',soundCueCustomDescPlaceholder:'Optional note, e.g. an easy laughing reaction',soundCueCustomAdd:'Add type',soundCueCustomSave:'Save',soundCueCustomDelete:'Delete type',soundCueCustomDeleteConfirm:'Delete this custom emotion type and its audio list?',soundCueCustomEmpty:'No custom emotion types yet. Add one first, then upload audio.',
      soundCueImport:'Import',soundCueExport:'Export',soundCueImportFailedPrefix:'Sound cue import failed',soundCueExportFailedPrefix:'Sound cue export failed',soundCueImportDone:'Sound cue settings imported',soundCueExportDone:'Sound cue settings exported',
      soundCueTypeReplyDoneTitle:'Agent reply finished',soundCueTypeReplyDoneDesc:'Normal style for the end of an assistant turn.',
      soundCueTypeThinkingTitle:'Thinking',soundCueTypeThinkingDesc:'A meaningful pondering sound while the agent starts thinking.',
      soundCueTypeConfusedTitle:'Agent confused',soundCueTypeConfusedDesc:'A very confused cue with a soft wobble.',
      soundCueTypeHelpTitle:'Agent seeking help',soundCueTypeHelpDesc:'Urgent but not harsh, with a slightly wronged feeling.',
      soundCueTypeConfidentTitle:'Agent confident',soundCueTypeConfidentDesc:'Cheerful and bright for high confidence.',
      soundCueTypeLowConfidenceTitle:'Agent low confidence',soundCueTypeLowConfidenceDesc:'Low and subdued for setbacks or weak confidence.',
      soundCueTypeIdeaTitle:'Agent sudden idea',soundCueTypeIdeaDesc:'A lively hmm-like cue for a sudden thought.',
      multiDescription:'Configure image generation, TTS voice assets, web search, and browser speech recognition. Keys are write-only; blank keeps the existing value.',
      multiTtsProfiles:'TTS voice models',multiSearchProfiles:'Web search providers',multiFieldProfileId:'Profile ID',multiFieldDisplayName:'Display Name',
      multiFieldProvider:'Provider',multiFieldEndpointPath:'Endpoint path',multiFieldMaxResults:'Max results',
      multiOptionSearchTavily:'Tavily',multiOptionSearchBrave:'Brave Search',multiOptionSearchFirecrawl:'Firecrawl',multiOptionSearchCustom:'Custom'
    });
    Object.assign(i18n.zh,{
      chatJumpBottom:'\u56de\u5230\u5e95\u90e8',soundCueDefaultLocked:'\u5185\u7f6e\u97f3\u9891\u4e0d\u80fd\u5220\u9664',
      soundCueTypeHappyTitle:'Agent \u5f00\u5fc3',soundCueTypeHappyDesc:'\u8f7b\u5feb\u660e\u4eae\uff0c\u7528\u4e8e\u771f\u7684\u6109\u5feb\u6216\u8f7b\u677e\u8d5e\u540c\u3002',
      soundCueTypeSadTitle:'Agent \u96be\u8fc7',soundCueTypeSadDesc:'\u67d4\u548c\u4e0b\u5760\uff0c\u8868\u793a\u5931\u843d\u3001\u9057\u61be\u6216\u4f4e\u843d\u3002',
      soundCueTypePerfunctoryTitle:'Agent \u6577\u884d',soundCueTypePerfunctoryDesc:'\u77ed\u3001\u5e73\u3001\u7565\u5e26\u201c\u55ef\u884c\u5427\u201d\u7684\u611f\u89c9\uff0c\u4f46\u4e0d\u5192\u72af\u3002',
      soundCueTypeConsideringTitle:'Agent \u7565\u5fae\u601d\u7d22',soundCueTypeConsideringDesc:'\u5f88\u77ed\u7684\u8f7b\u601d\u8003\uff0c\u6bd4 thinking \u66f4\u8f7b\u3002',
      soundCueTypeWorkingHardTitle:'Agent \u52aa\u529b\u4e2d',soundCueTypeWorkingHardDesc:'\u6709\u8282\u594f\u7684\u5c0f\u63a8\u8fdb\u611f\uff0c\u8868\u793a\u6b63\u5728\u53d1\u529b\u89e3\u51b3\u3002',
      soundCueTypeTiredTitle:'Agent \u75b2\u60eb',soundCueTypeTiredDesc:'\u6c14\u529b\u4e0d\u8db3\u3001\u4e0b\u6c89\uff0c\u4f46\u4e0d\u9634\u90c1\u3002',
      soundCueTypeEnergizedTitle:'Agent \u7cbe\u529b\u5145\u6c9b',soundCueTypeEnergizedDesc:'\u6e05\u8106\u6709\u51b2\u52b2\uff0c\u8868\u793a\u72b6\u6001\u62c9\u6ee1\u3002',
      soundCueTypeAngryTitle:'Agent \u6124\u6012',soundCueTypeAngryDesc:'\u77ed\u4fc3\u3001\u538b\u7740\u706b\u6c14\uff0c\u4e0d\u523a\u8033\u3002',
      soundCueTypeRelievedTitle:'Agent \u91ca\u7136',soundCueTypeRelievedDesc:'\u50cf\u677e\u4e00\u53e3\u6c14\uff0c\u9002\u5408\u538b\u529b\u89e3\u9664\u3002',
      soundCueTypeAwkwardTitle:'Agent \u5c34\u5c2c',soundCueTypeAwkwardDesc:'\u8f7b\u5fae\u5361\u987f\u548c\u5c40\u4fc3\u611f\u3002',
      soundCueTypeSurprisedTitle:'Agent \u60ca\u8bb6',soundCueTypeSurprisedDesc:'\u4e0a\u626c\u3001\u7a81\u7136\u4eae\u4e00\u4e0b\u7684\u53cd\u5e94\u3002',
      soundCueTypeApologeticTitle:'Agent \u9053\u6b49',soundCueTypeApologeticDesc:'\u67d4\u548c\u6536\u655b\uff0c\u5e26\u4e00\u70b9\u4f4e\u5934\u611f\u3002',
      soundCueTypeSkepticalTitle:'Agent \u6000\u7591',soundCueTypeSkepticalDesc:'\u77ed\u4fc3\u7591\u95ee\u611f\uff0c\u50cf\u201c\u55ef\uff1f\u201d\u3002',
      soundCueTypeAlertTitle:'Agent \u8b66\u89c9',soundCueTypeAlertDesc:'\u63d0\u9192\u611f\u5f3a\uff0c\u4f46\u4e0d\u62a5\u8b66\u5316\u3002',
      soundCueTypeCelebrateTitle:'Agent \u5e86\u795d',soundCueTypeCelebrateDesc:'\u5c0f\u80dc\u5229\u548c\u5b8c\u6210\u611f\uff0c\u6bd4 confident \u66f4\u5916\u653e\u3002',
      soundCueTypeGentleTitle:'Agent \u6e29\u67d4',soundCueTypeGentleDesc:'\u8f7b\u3001\u6696\u3001\u5b89\u629a\u578b\u3002',
      soundCueTypePlayfulTitle:'Agent \u8c03\u76ae',soundCueTypePlayfulDesc:'\u7075\u52a8\u3001\u4fcf\u76ae\u3001\u6709\u5c0f\u8f6c\u6298\u3002'
    });
    Object.assign(i18n.en,{
      chatJumpBottom:'Jump to bottom',soundCueDefaultLocked:'Built-in asset cannot be removed',
      soundCueTypeHappyTitle:'Agent happy',soundCueTypeHappyDesc:'Light and bright for genuine delight or easy agreement.',
      soundCueTypeSadTitle:'Agent sad',soundCueTypeSadDesc:'Soft and downward for sadness, regret, or a low mood.',
      soundCueTypePerfunctoryTitle:'Agent perfunctory',soundCueTypePerfunctoryDesc:'Short, flat, and slightly "fine then" without being rude.',
      soundCueTypeConsideringTitle:'Agent considering',soundCueTypeConsideringDesc:'A very short light-thought cue, gentler than thinking.',
      soundCueTypeWorkingHardTitle:'Agent working hard',soundCueTypeWorkingHardDesc:'Small rhythmic push for active effort and problem solving.',
      soundCueTypeTiredTitle:'Agent tired',soundCueTypeTiredDesc:'Low energy and sinking, but not gloomy.',
      soundCueTypeEnergizedTitle:'Agent energized',soundCueTypeEnergizedDesc:'Crisp and driven for a fully charged state.',
      soundCueTypeAngryTitle:'Agent angry',soundCueTypeAngryDesc:'Short and restrained, irritated but not harsh.',
      soundCueTypeRelievedTitle:'Agent relieved',soundCueTypeRelievedDesc:'A small release of pressure.',
      soundCueTypeAwkwardTitle:'Agent awkward',soundCueTypeAwkwardDesc:'Slightly stuck and self-conscious.',
      soundCueTypeSurprisedTitle:'Agent surprised',soundCueTypeSurprisedDesc:'Rising and bright for a sudden reaction.',
      soundCueTypeApologeticTitle:'Agent apologetic',soundCueTypeApologeticDesc:'Soft and restrained, with a small bowing feeling.',
      soundCueTypeSkepticalTitle:'Agent skeptical',soundCueTypeSkepticalDesc:'A short questioning cue, like "hm?".',
      soundCueTypeAlertTitle:'Agent alert',soundCueTypeAlertDesc:'A clear warning-style cue without becoming an alarm.',
      soundCueTypeCelebrateTitle:'Agent celebrating',soundCueTypeCelebrateDesc:'A small victory cue, more outward than confident.',
      soundCueTypeGentleTitle:'Agent gentle',soundCueTypeGentleDesc:'Light, warm, and soothing.',
      soundCueTypePlayfulTitle:'Agent playful',soundCueTypePlayfulDesc:'Lively, teasing, and nimble.'
    });
    Object.assign(i18n.zh,{
      runtimeEventsTitle:'后台事件',
      runtimeEventsDesc:'当前 Agent 最近的 subagent、调度和恢复事件；按完成、未完成、跳过和剩余未完成项同步汇总。',
      runtimeEventsReload:'刷新事件',
      runtimeEventsEmpty:'暂无后台事件',
      runtimeEventsCompleted:'已完成',
      runtimeEventsUnfinished:'未完成',
      runtimeEventsSkipped:'已跳过',
      runtimeEventsFailed:'失败',
      runtimeEventsRemaining:'剩余',
      runtimeEventsRemainingTitle:'剩余未完成项',
      runtimeStatusCompleted:'已完成',
      runtimeStatusUnfinished:'未完成',
      runtimeStatusSkipped:'已跳过',
      runtimeStatusFailed:'失败',
      runtimeAdvice_no_action_needed:'无需操作。',
      runtimeAdvice_wait_for_completion:'等待任务完成。',
      runtimeAdvice_retry_manual:'可手动重试；如果连续失败，请检查 API Key、网络和任务输入。',
      runtimeAdvice_review_memory:'请检查记忆文件，必要时从快照回滚。',
      runtimeAdvice_review_skills:'请检查技能内容和资源文件，必要时重新验证。',
      runtimeAdvice_review_validation_report:'请查看验证报告并按建议修复。',
      runtimeAdvice_review_result:'请查看任务结果。',
      runtimeAdvice_wait_for_idle:'当前用户回合结束后会自动重试。',
      runtimeAdvice_review_import_source:'请检查导入材料是否足够具体、可复用。'
    });
    Object.assign(i18n.zh,{runtimeEventsAgentLabel:'Agent',runtimeEventsAgentMeta:'事件来源'});
    Object.assign(i18n.en,{
      runtimeEventsTitle:'Background Events',
      runtimeEventsDesc:'Recent subagent, scheduler, and recovery events for the selected agent, summarized by completed, unfinished, skipped, and remaining work.',
      runtimeEventsReload:'Reload Events',
      runtimeEventsAgentLabel:'Agent',
      runtimeEventsAgentMeta:'event source',
      runtimeEventsEmpty:'No background events yet',
      runtimeEventsCompleted:'Completed',
      runtimeEventsUnfinished:'Unfinished',
      runtimeEventsSkipped:'Skipped',
      runtimeEventsFailed:'Failed',
      runtimeEventsRemaining:'Remaining',
      runtimeEventsRemainingTitle:'Remaining unfinished items',
      runtimeStatusCompleted:'completed',
      runtimeStatusUnfinished:'unfinished',
      runtimeStatusSkipped:'skipped',
      runtimeStatusFailed:'failed'
    });
    Object.assign(i18n.zh,{
      privacyAccessTitle:'\u9690\u79c1\u8bbf\u95ee',
      privacyAccessDesc:'\u5141\u8bb8 agent \u5728\u4efb\u52a1\u9700\u8981\u65f6\u8bfb\u53d6\u7528\u6237\u79c1\u6709\u6587\u4ef6\u3002\u5728\u793e\u4ea4\u8f6f\u4ef6\u3001\u90ae\u7bb1\u3001\u8bba\u575b\u3001\u672a\u77e5\u7f51\u9875\u6216\u63d0\u793a\u8bcd\u6ce8\u5165\u98ce\u9669\u9ad8\u7684\u73af\u5883\u4e2d\u5efa\u8bae\u5173\u95ed\u3002',
      privacyAccessToggleLabel:'\u5141\u8bb8\u8bbf\u95ee\u9690\u79c1\u6570\u636e',
      privacyAccessOn:'\u5df2\u5f00\u542f\uff1a\u4ec5\u653e\u5f00\u4efb\u52a1\u5fc5\u8981\u7684\u9690\u79c1\u6570\u636e\u8bfb\u53d6\uff0c\u4ecd\u7981\u6b62\u6cc4\u9732\u5bc6\u7801/token/cookie/API key \u539f\u503c\u548c\u4fee\u6539 Matdance \u5185\u90e8\u3002',
      privacyAccessOff:'\u9ed8\u8ba4\u5173\u95ed\uff1aagent \u53ea\u80fd\u8bfb\u53d6 workspace \u548c\u53ef\u9884\u89c8\u8fd0\u884c\u8f93\u51fa\u3002',
      privacyAccessSaveFailed:'\u9690\u79c1\u8bbf\u95ee\u8bbe\u7f6e\u4fdd\u5b58\u5931\u8d25',
      labNoResult:'\u6682\u65e0\u7ed3\u679c\u3002',
      previewOpenFull:'\u6253\u5f00\u5b8c\u6574\u9884\u89c8',
      previewLoading:'\u52a0\u8f7d\u4e2d...',
      previewLoadFailed:'\u52a0\u8f7d\u5931\u8d25',
      previewLoadImageFailed:'\u56fe\u7247\u52a0\u8f7d\u5931\u8d25',
      previewLoadMarkdownFailed:'Markdown \u52a0\u8f7d\u5931\u8d25',
      previewLoadFileFailed:'\u6587\u4ef6\u52a0\u8f7d\u5931\u8d25',
      previewDocument:'\u6587\u6863\u9884\u89c8',
      previewOpenBrowser:'\u5728\u6d4f\u89c8\u5668\u4e2d\u6253\u5f00',
      previewUnavailable:'\u6b64\u6587\u4ef6\u7c7b\u578b\u6682\u4e0d\u652f\u6301\u9884\u89c8',
      previewUnavailableInline:'\u6b64\u6587\u4ef6\u7c7b\u578b\u65e0\u6cd5\u5185\u8054\u9884\u89c8\u3002',
      previewDownload:'\u4e0b\u8f7d\u6587\u4ef6',
      previewOpenRaw:'\u6253\u5f00\u539f\u59cb\u6587\u4ef6',
      ttsErrorTitle:'\u8bed\u97f3\u64ad\u653e\u5931\u8d25',
      ttsErrorFallback:'\u8bed\u97f3\u5408\u6210\u6216\u64ad\u653e\u5931\u8d25\u3002',
      ttsPlayGenerated:'\u64ad\u653e\u5df2\u751f\u6210\u8bed\u97f3',
      ttsGenerateForMessage:'\u4e3a\u6b64\u6d88\u606f\u751f\u6210\u8bed\u97f3',
      ttsNoSpeakableText:'\u6ca1\u6709\u53ef\u7528\u4e8e\u8bed\u97f3\u5408\u6210\u7684\u6587\u672c\u3002',
      ttsNoPlayableAudio:'TTS \u6ca1\u6709\u8fd4\u56de\u53ef\u64ad\u653e\u7684\u97f3\u9891\u3002',
      sttFailedPrefix:'\u8bed\u97f3\u8bc6\u522b\u5931\u8d25'
    });
    Object.assign(i18n.en,{
      privacyAccessTitle:'Privacy Access',
      privacyAccessDesc:'Allow agents to read user-private files only when a task needs it. Keep this off in social apps, mail, forums, unknown pages, or prompt-injection-heavy environments.',
      privacyAccessToggleLabel:'Allow private data access',
      privacyAccessOn:'Enabled: only task-needed private data reads are loosened; raw passwords/tokens/cookies/API keys and Matdance internals remain blocked.',
      privacyAccessOff:'Default off: agents can read only workspace files and preview-safe runtime output.',
      privacyAccessSaveFailed:'Privacy access setting save failed',
      labNoResult:'No result yet.',
      previewOpenFull:'Open full preview',
      previewLoading:'Loading...',
      previewLoadFailed:'Failed to load',
      previewLoadImageFailed:'Failed to load image',
      previewLoadMarkdownFailed:'Failed to load Markdown',
      previewLoadFileFailed:'Failed to load file',
      previewDocument:'Document preview',
      previewOpenBrowser:'Open in browser',
      previewUnavailable:'Preview is not available for this file type',
      previewUnavailableInline:'This file type cannot be previewed inline.',
      previewDownload:'Download file',
      previewOpenRaw:'Open raw',
      ttsErrorTitle:'Speech Playback Failed',
      ttsErrorFallback:'Speech synthesis or playback failed.',
      ttsPlayGenerated:'Play generated speech',
      ttsGenerateForMessage:'Generate speech for this message',
      ttsNoSpeakableText:'No speakable text for TTS.',
      ttsNoPlayableAudio:'TTS returned no playable audio.',
      sttFailedPrefix:'Speech recognition failed'
    });
    Object.assign(i18n.zh,{
      skillValidationTitle:'\u6280\u80fd\u9a8c\u8bc1',
      skillValidationDesc:'\u5168\u5c40\u961f\u5217\u4e32\u884c\u9a8c\u8bc1\u6280\u80fd\uff0c\u907f\u514d\u7a7a\u95f2\u540e\u53f0\u68c0\u67e5\u6d88\u8017\u8fc7\u591a\u8bf7\u6c42\u3002',
      skillValidationEnabledLabel:'\u542f\u7528\u81ea\u52a8\u9a8c\u8bc1',
      skillValidationIntervalLabel:'\u9a8c\u8bc1\u95f4\u9694',
      skillValidationIntervalMeta:'\u5c0f\u65f6',
      skillValidationBatchLabel:'\u6bcf\u8f6e\u6570\u91cf',
      skillValidationBatchMeta:'\u6700\u591a 3',
      skillValidationStateText:'\u6bcf {hours} \u5c0f\u65f6\u6700\u591a\u4e32\u884c\u9a8c\u8bc1 {count} \u4e2a\u6280\u80fd\u3002',
      skillValidationDisabledState:'\u81ea\u52a8\u9a8c\u8bc1\u5df2\u5173\u95ed\uff1b\u624b\u52a8\u9a8c\u8bc1\u4e0d\u53d7\u5f71\u54cd\u3002',
      skillValidationSaveFailed:'\u6280\u80fd\u9a8c\u8bc1\u8bbe\u7f6e\u4fdd\u5b58\u5931\u8d25'
    });
    Object.assign(i18n.en,{
      skillValidationTitle:'Skill Validation',
      skillValidationDesc:'Queue automatic validation globally; each window runs serial checks and avoids repeated needs_changes requests.',
      skillValidationEnabledLabel:'Enable automatic validation',
      skillValidationIntervalLabel:'Validation Interval',
      skillValidationIntervalMeta:'hours',
      skillValidationBatchLabel:'Batch Size',
      skillValidationBatchMeta:'max 3',
      skillValidationStateText:'Every {hours} hours, validate up to {count} skill(s) serially.',
      skillValidationDisabledState:'Automatic validation is off; manual validation still runs immediately.',
      skillValidationSaveFailed:'Skill validation setting save failed'
    });
    function t(key, fallback) {
      const table = i18n[state.lang] ?? i18n.zh;
      return table[key] ?? i18n.zh[key] ?? fallback ?? key;
    }
    Object.assign(i18n.zh,{
      sessionNoticeSuffix:'\u4e13\u5c5e\u901a\u77e5',
      sessionReadOnlyHint:'\u8fd9\u662f\u5b9a\u65f6\u4efb\u52a1\u4e13\u5c5e\u901a\u77e5\u4f1a\u8bdd\uff0c\u4ec5\u63a5\u6536\u7ed3\u679c\u6295\u9012\u3002',
      sessionReadOnlyPlaceholder:'\u53ea\u8bfb\u901a\u77e5\u4f1a\u8bdd',
      scheduleTargetNotification:'\u4e13\u5c5e\u901a\u77e5\u4f1a\u8bdd',
      scheduleManualTestLabel:'\u624b\u52a8\u6d4b\u8bd5',
      noticeTriggerLabel:'\u89e6\u53d1',
      scheduleSystemTestBlocked:'\u7cfb\u7edf\u5185\u7f6e\u4efb\u52a1\u4e0d\u652f\u6301\u624b\u52a8\u6d4b\u8bd5\u3002',
      scheduleTestingTitle:'\u6b63\u5728\u6d4b\u8bd5\u5b9a\u65f6\u4efb\u52a1',
      scheduleTestingStage:'\u6b63\u5728\u83b7\u53d6\u8fd0\u884c\u8fdb\u5ea6...',
      scheduleTestingDeliver:'\u6d4b\u8bd5\u4f1a\u771f\u5b9e\u6267\u884c\u5e76\u6295\u9012\u7ed3\u679c\uff0c\u4f46\u4e0d\u4f1a\u63a8\u8fdb\u539f\u5b9a nextRunAt\u3002',
      scheduleTestDone:'\u6d4b\u8bd5\u5b8c\u6210\uff0c\u7ed3\u679c\u5df2\u6309\u4efb\u52a1\u76ee\u6807\u6295\u9012\u3002',
      scheduleTestFailed:'\u6d4b\u8bd5\u5931\u8d25'
    });
    Object.assign(i18n.en,{
      sessionNoticeSuffix:'Dedicated notice',
      sessionReadOnlyHint:'This scheduled-task notification session only receives result deliveries.',
      sessionReadOnlyPlaceholder:'Read-only notification session',
      scheduleTargetNotification:'Dedicated Notification Session',
      scheduleManualTestLabel:'Manual test',
      noticeTriggerLabel:'Trigger',
      scheduleSystemTestBlocked:'System scheduled tasks cannot be manually tested.',
      scheduleTestingTitle:'Testing scheduled task',
      scheduleTestingStage:'Reading run progress...',
      scheduleTestingDeliver:'This test does real work and delivers results, but does not advance the original nextRunAt.',
      scheduleTestDone:'Test completed; results were delivered to the configured targets.',
      scheduleTestFailed:'Test failed'
    });

    const commands = [
      { name: '/new-session', aliases: ['/new'], descriptionKey: 'cmdNewSessionDesc', run: async function() { await createSession(); return t('cmdNewSessionDone'); } },
      { name: '/refresh', aliases: ['/reload'], descriptionKey: 'cmdRefreshDesc', run: async function() { await loadAgents(); return t('cmdRefreshDone'); } },
      { name: '/status', aliases: ['/stats'], descriptionKey: 'cmdStatusDesc', run: async function() { await loadSession(); return t('cmdStatusDone'); } },
      { name: '/clear', aliases: [], descriptionKey: 'cmdClearDesc', run: async function() { showEmpty(t('cmdClearEmpty')); return t('cmdClearDone'); } },
      { name: '/home', aliases: ['/tags'], descriptionKey: 'cmdHomeDesc', run: async function() { await switchTab('home'); return t('cmdHomeDone'); } },
      { name: '/agent', aliases: ['/config'], descriptionKey: 'cmdAgentDesc', run: async function() { await switchTab('agent'); return t('cmdAgentDone'); } },
      { name: '/settings', aliases: ['/set'], descriptionKey: 'cmdSettingsDesc', run: async function() { await switchTab('settings'); return t('cmdSettingsDone'); } },
      { name: '/lab', aliases: ['/debug'], descriptionKey: 'cmdLabDesc', run: async function() { await switchTab('lab'); return t('cmdLabDone'); } },
      { name: '/memory', aliases: ['/mem'], descriptionKey: 'cmdMemoryDesc', run: async function() { await switchTab('memory'); return t('cmdMemoryDone'); } },
      { name: '/chat', aliases: [], descriptionKey: 'cmdChatDesc', run: async function() { await switchTab('chat'); return t('cmdChatDone'); } },
      { name: '/help', aliases: ['/?'], descriptionKey: 'cmdHelpDesc', run: async function() { addMessage('assistant', commandHelpMarkdown(), t('cmdHelpTitle')); return t('cmdHelpDone'); } }
    ];
    const frames = ['▖','▘','▝','▗','◢','◣','◤','◥','▪','■','□','■'];
    let frame = 0;
    const tagWorlds = [
      { id: 'chat', nameKey: 'tagChatName', descriptionKey: 'tagChatDescription', pos: [-260, -20, 190], radius: 42, color: '#67f7b1', accent: '#64dbff', enabled: true },
      { id: 'agent', nameKey: 'tagAgentName', descriptionKey: 'tagAgentDescription', pos: [250, 22, 150], radius: 40, color: '#a78bfa', accent: '#64dbff', enabled: true },
      { id: 'schedule', nameKey: 'tagScheduleName', descriptionKey: 'tagScheduleDescription', pos: [22, 148, 245], radius: 37, color: '#ffd166', accent: '#67f7b1', enabled: true },
      { id: 'settings', nameKey: 'tagSettingsName', descriptionKey: 'tagSettingsDescription', pos: [28, -154, 230], radius: 35, color: '#64dbff', accent: '#67f7b1', enabled: true },
      { id: 'skills', nameKey: 'tagSkillsName', descriptionKey: 'tagSkillsDescription', pos: [-330, 130, -120], radius: 34, color: '#80a7ff', accent: '#aab8d0', enabled: true },
      { id: 'lab', nameKey: 'tagLabName', descriptionKey: 'tagLabDescription', pos: [-48, 250, -235], radius: 33, color: '#ff7aa2', accent: '#67f7b1', enabled: true },
      { id: 'memory', nameKey: 'tagMemoryName', descriptionKey: 'tagMemoryDescription', pos: [330, -130, -150], radius: 36, color: '#ffd166', accent: '#67f7b1', enabled: true }
    ];
    function tagName(world) { return t(world?.nameKey, world?.name ?? world?.id ?? ''); }
    function tagDescription(world) { return t(world?.descriptionKey, world?.description ?? ''); }
    const starScene = {
      initialized: false,
      canvas: null,
      ctx: null,
      width: 1,
      height: 1,
      dpr: 1,
      stars: [],
      frame: 0,
      hover: null,
      pointer: { x: -9999, y: -9999 },
      drag: null,
      moved: 0,
      lastInput: performance.now(),
      warp: null,
      camera: { rotX: -0.16, rotY: 0.22, zoom: 760, panX: 0, panY: 0 },
      home: { rotX: -0.16, rotY: 0.22, zoom: 760, panX: 0, panY: 0 },
      reducedMotion: window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches
    };
    const STAR_BASE_COUNT = 620;
    const STAR_BASE_RANGE_X = 1400;
    const STAR_BASE_RANGE_Y = 900;
    const STAR_BASE_RANGE_Z = 1200;
    const STAR_PAN_LIMIT_SCALE = 1;
    const STAR_PAN_LIMIT_X = STAR_BASE_RANGE_X * STAR_PAN_LIMIT_SCALE / 2;
    const STAR_PAN_LIMIT_Y = STAR_BASE_RANGE_Y * STAR_PAN_LIMIT_SCALE / 2;

    function clamp(value, min, max) { return Math.max(min, Math.min(max, value)); }
    function lerp(a, b, t) { return a + (b - a) * t; }

    function smoothstep(value) {
      const t = clamp(value, 0, 1);
      return t * t * (3 - 2 * t);
    }

    function cloneCamera(camera) {
      return { rotX: camera.rotX, rotY: camera.rotY, zoom: camera.zoom, panX: camera.panX, panY: camera.panY };
    }

    function cameraForWorld(world) {
      const [x, y, z] = world.pos;
      const rotY = Math.atan2(x, z);
      const planar = Math.max(1, Math.hypot(x, z));
      const distance = Math.hypot(x, y, z);
      const approachDepth = Math.max(138, world.radius * 3.1);
      return {
        rotX: clamp(Math.atan2(y, planar), -1.05, .82),
        rotY,
        zoom: 560,
        approachZoom: approachDepth - distance,
        panX: 0,
        panY: 0
      };
    }

    function updateWarpCamera(now) {
      const warp = starScene.warp;
      if (!warp) return 0;
      if (!warp.from) return 0;
      if (!warp.target) return 0;
      const progress = clamp((now - warp.start) / warp.duration, 0, 1);
      const aim = smoothstep(progress / .34);
      const launch = smoothstep((progress - .34) / .66);
      const arrival = smoothstep((progress - .58) / .42);
      const approachZoom = warp.target.approachZoom ?? 220;
      const cam = starScene.camera;
      cam.rotX = lerp(warp.from.rotX, warp.target.rotX, aim);
      cam.rotY = lerp(warp.from.rotY, warp.target.rotY, aim);
      const lockZoom = lerp(warp.from.zoom, warp.target.zoom, aim);
      const cruiseZoom = lerp(lockZoom, 420, launch);
      cam.zoom = lerp(cruiseZoom, approachZoom, arrival);
      cam.panX = lerp(warp.from.panX, 0, aim);
      cam.panY = lerp(warp.from.panY, 0, aim);
      return progress;
    }

    function positiveModulo(value, size) {
      return ((value % size) + size) % size;
    }

    function relativisticStarColor(radial, beta, alpha) {
      const heat = clamp(radial * beta, 0, 1);
      const ahead = clamp((1 - radial) * beta, 0, 1);
      const r = Math.round(clamp(145 + 95 * heat - 25 * ahead, 80, 255));
      const g = Math.round(clamp(210 + 45 * ahead - 105 * heat, 90, 255));
      const b = Math.round(clamp(245 + 10 * ahead - 170 * heat, 70, 255));
      return `rgba(${r},${g},${b},${alpha})`;
    }

    function initStarMap() {
      const canvas = $('starMap');
      if (!canvas || starScene.initialized) return;
      starScene.initialized = true;
      starScene.canvas = canvas;
      starScene.ctx = canvas.getContext('2d');
      buildStars();
      resizeStarMap();
      window.addEventListener('resize', resizeStarMap);
      canvas.addEventListener('contextmenu', event => event.preventDefault());
      canvas.addEventListener('pointerdown', onStarPointerDown);
      canvas.addEventListener('pointerleave', onStarPointerLeave);
      window.addEventListener('pointermove', onStarPointerMove);
      window.addEventListener('pointerup', onStarPointerUp);
      canvas.addEventListener('wheel', onStarWheel, { passive: false });
      startStarMap();
    }

    function startStarMap() {
      if (!starScene.ctx || starScene.frame) return;
      starScene.frame = requestAnimationFrame(renderStarMap);
    }

    function buildStars() {
      starScene.stars = Array.from({ length: STAR_BASE_COUNT }, (_, index) => {
        const layer = index % 3;
        return {
          x: (Math.random() - .5) * STAR_BASE_RANGE_X,
          y: (Math.random() - .5) * STAR_BASE_RANGE_Y,
          z: (Math.random() - .5) * STAR_BASE_RANGE_Z,
          size: layer === 0 ? 1.6 : layer === 1 ? 1.1 : .7,
          alpha: layer === 0 ? .88 : layer === 1 ? .52 : .28,
          twinkle: Math.random() * Math.PI * 2,
          angle: Math.random() * Math.PI * 2,
          lane: Math.random(),
          drift: (Math.random() - .5) * .18
        };
      });
    }

    function clampStarPan() {
      starScene.camera.panX = clamp(starScene.camera.panX, -STAR_PAN_LIMIT_X, STAR_PAN_LIMIT_X);
      starScene.camera.panY = clamp(starScene.camera.panY, -STAR_PAN_LIMIT_Y, STAR_PAN_LIMIT_Y);
    }

    function resizeStarMap() {
      const canvas = starScene.canvas;
      if (!canvas) return;
      const rect = canvas.getBoundingClientRect();
      if (Math.min(rect.width, rect.height) < 2) return;
      starScene.dpr = Math.min(window.devicePixelRatio || 1, 2);
      starScene.width = Math.max(1, rect.width);
      starScene.height = Math.max(1, rect.height);
      canvas.width = Math.floor(starScene.width * starScene.dpr);
      canvas.height = Math.floor(starScene.height * starScene.dpr);
      starScene.ctx.setTransform(starScene.dpr, 0, 0, starScene.dpr, 0, 0);
    }

    function rotatePoint(pos) {
      const cam = starScene.camera;
      const cosY = Math.cos(cam.rotY), sinY = Math.sin(cam.rotY);
      const cosX = Math.cos(cam.rotX), sinX = Math.sin(cam.rotX);
      let x = pos[0] * cosY - pos[2] * sinY;
      let z = pos[0] * sinY + pos[2] * cosY;
      let y = pos[1] * cosX - z * sinX;
      z = pos[1] * sinX + z * cosX;
      return { x, y, z };
    }

    function projectPoint(pos) {
      const cam = starScene.camera;
      const rotated = rotatePoint(pos);
      const depth = rotated.z + cam.zoom;
      if (depth < 80) return null;
      const focal = Math.min(starScene.width, starScene.height) * .9;
      const scale = focal / depth;
      return {
        x: starScene.width / 2 + (rotated.x + cam.panX) * scale,
        y: starScene.height / 2 + (rotated.y + cam.panY) * scale,
        z: depth,
        scale
      };
    }

    function onStarPointerDown(event) {
      if (state.activeTab !== 'home' || starScene.warp) return;
      event.preventDefault();
      starScene.canvas.classList.add('dragging');
      starScene.drag = { mode: event.button === 2 ? 'pan' : 'rotate', x: event.clientX, y: event.clientY };
      starScene.moved = 0;
      starScene.lastInput = performance.now();
    }

    function onStarPointerMove(event) {
      if (!starScene.canvas) return;
      const rect = starScene.canvas.getBoundingClientRect();
      starScene.pointer.x = event.clientX - rect.left;
      starScene.pointer.y = event.clientY - rect.top;
      if (state.activeTab !== 'home') return;
      if (starScene.drag) {
        const dx = event.clientX - starScene.drag.x;
        const dy = event.clientY - starScene.drag.y;
        starScene.drag.x = event.clientX;
        starScene.drag.y = event.clientY;
        starScene.moved += Math.abs(dx) + Math.abs(dy);
        if (starScene.drag.mode === 'pan') {
          starScene.camera.panX += dx * 1.65;
          starScene.camera.panY += dy * 1.65;
          clampStarPan();
        } else {
          starScene.camera.rotY += dx * .006;
          starScene.camera.rotX = clamp(starScene.camera.rotX + dy * .004, -1.05, .82);
        }
        starScene.lastInput = performance.now();
      }
      detectPlanetHover();
    }

    function onStarPointerUp() {
      if (!starScene.drag) return;
      const canLaunch = starScene.drag.mode === 'rotate' && starScene.moved < 8 && starScene.hover && starScene.hover.enabled;
      starScene.drag = null;
      starScene.canvas?.classList.remove('dragging');
      starScene.lastInput = performance.now();
      if (canLaunch) startWarp(starScene.hover.id);
    }


    function onStarPointerLeave() {
      if (!starScene.canvas || starScene.drag || state.activeTab !== 'home') return;
      starScene.pointer.x = -9999;
      starScene.pointer.y = -9999;
      detectPlanetHover();
    }

    function onStarWheel(event) {
      if (state.activeTab !== 'home' || starScene.warp) return;
      event.preventDefault();
      starScene.camera.zoom = clamp(starScene.camera.zoom + event.deltaY * .46, 430, 1250);
      starScene.lastInput = performance.now();
    }

    function detectPlanetHover() {
      let best = null;
      let bestDistance = Infinity;
      for (const world of tagWorlds) {
        const screen = projectPoint(world.pos);
        if (!screen) continue;
        const radius = Math.max(14, world.radius * screen.scale);
        const distance = Math.hypot(starScene.pointer.x - screen.x, starScene.pointer.y - screen.y);
        if (distance < radius + 16 && distance < bestDistance) {
          best = world;
          bestDistance = distance;
        }
      }
      starScene.canvas?.classList.toggle('hot-target', !!(best && best.enabled));
      if (starScene.hover !== best) {
        starScene.hover = best;
        updatePlanetHud(best);
      }
    }

    function updatePlanetHud(world) {
      const hud = $('planetHud');
      if (!hud) return;
      hud.classList.toggle('active', !!world);
      if (!world) {
        hud.innerHTML = '<span class="hud-kicker">' + escapeHtml(t('hudAwaiting')) + '</span><strong>' + escapeHtml(t('hudChoose')) + '</strong><p>' + escapeHtml(t('hudHelp')) + '</p>';
        return;
      }
      hud.innerHTML = '<span class="hud-kicker">' + escapeHtml(world.enabled ? t('hudLanding') : t('hudReserved')) + '</span><strong>' + escapeHtml(tagName(world)) + '</strong><p>' + escapeHtml(tagDescription(world)) + '</p>';
    }

    function drawStarMapBackground(ctx, now) {
      const gradient = ctx.createRadialGradient(starScene.width * .5, starScene.height * .48, 0, starScene.width * .5, starScene.height * .5, Math.max(starScene.width, starScene.height) * .72);
      gradient.addColorStop(0, `rgba(8,22,38,.78)`);
      gradient.addColorStop(.44, `rgba(3,8,18,.92)`);
      gradient.addColorStop(1, `rgba(1,3,8,1)`);
      ctx.fillStyle = gradient;
      ctx.fillRect(0, 0, starScene.width, starScene.height);

      const warpProgress = starScene.warp ? clamp((now - starScene.warp.start) / starScene.warp.duration, 0, 1) : 0;
      const launchProgress = starScene.warp ? smoothstep((warpProgress - .38) / .62) : 0;
      const beta = launchProgress * .985;
      const gamma = beta ? 1 / Math.sqrt(Math.max(.03, 1 - beta * beta)) : 1;
      const flowTravel = starScene.warp ? Math.max(0, now - starScene.warp.start - starScene.warp.duration * .38) * (.42 + beta * beta * 4.8) : 0;
      const warpFocus = starScene.warp?.world ? projectPoint(starScene.warp.world.pos) : null;
      const focusX = warpFocus ? warpFocus.x : starScene.width / 2;
      const focusY = warpFocus ? warpFocus.y : starScene.height / 2;
      const occlusionRadius = warpFocus && starScene.warp?.world ? Math.max(24, starScene.warp.world.radius * warpFocus.scale + 18) : 0;

      if (starScene.warp) {
        const cone = ctx.createRadialGradient(focusX, focusY, 0, focusX, focusY, Math.max(starScene.width, starScene.height) * .62);
        cone.addColorStop(0, `rgba(215,255,255,${.20 + beta * .28})`);
        cone.addColorStop(.34, `rgba(100,219,255,${.10 + beta * .18})`);
        cone.addColorStop(1, `rgba(255,98,146,0)`);
        ctx.fillStyle = cone;
        ctx.fillRect(0, 0, starScene.width, starScene.height);
      }

      ctx.globalCompositeOperation = starScene.warp ? `lighter` : `source-over`;
      ctx.shadowBlur = launchProgress ? 1.2 : 0;
      ctx.shadowColor = `rgba(100,219,255,.32)`;
      ctx.lineCap = launchProgress ? `round` : `butt`;
      for (let starIndex = 0; starIndex < starScene.stars.length; starIndex++) {
        if (launchProgress > 0 && starIndex % 2 === 1) continue;
        const star = starScene.stars[starIndex];
        if (launchProgress > 0) {
          const maxDistance = Math.hypot(starScene.width, starScene.height) * .72 + 260;
          const angle = star.angle + Math.sin(now * .00045 + star.twinkle) * star.drift * launchProgress;
          const directionX = Math.cos(angle);
          const directionY = Math.sin(angle);
          const flowDistance = positiveModulo(star.lane * maxDistance + flowTravel, maxDistance);
          const distance = Math.max(8, flowDistance);
          const radial = clamp(distance / maxDistance, 0, 1);
          const alpha = clamp(star.alpha * (.22 + launchProgress * .58) * (1 - radial * .26), .035, .72);
          const color = relativisticStarColor(radial, beta, alpha);
          const stretch = (42 + gamma * 42) * launchProgress * launchProgress * (1.08 - radial * .26);
          let tailDistance = Math.max(1, distance - stretch * 1.08);
          const headDistance = Math.min(maxDistance + stretch, distance + stretch * .18);
          if (headDistance <= occlusionRadius) continue;
          tailDistance = Math.max(tailDistance, occlusionRadius);
          const tailX = focusX + directionX * tailDistance;
          const tailY = focusY + directionY * tailDistance;
          const headX = focusX + directionX * headDistance;
          const headY = focusY + directionY * headDistance;
          const trail = ctx.createLinearGradient(tailX, tailY, headX, headY);
          trail.addColorStop(0, `rgba(120,190,255,${alpha * .035})`);
          trail.addColorStop(.64, color);
          trail.addColorStop(1, `rgba(235,255,255,${clamp(alpha * (.78 + beta * .38), .12, .68)})`);
          ctx.strokeStyle = trail;
          ctx.lineWidth = Math.max(.38, star.size * (.24 + beta * .56) * (1.04 - radial * .36));
          ctx.beginPath();
          ctx.moveTo(tailX, tailY);
          ctx.lineTo(headX, headY);
          ctx.stroke();
          continue;
        }
        const screen = projectPoint([star.x, star.y, star.z]);
        if (!screen) continue;
        const pulse = .55 + Math.sin(now * .0018 + star.twinkle) * .25;
        const alpha = clamp(star.alpha * pulse, .05, 1);
        const size = Math.max(.45, star.size * screen.scale * 2.2);
        ctx.fillStyle = `rgba(190,255,230,${alpha})`;
        ctx.beginPath();
        ctx.arc(screen.x, screen.y, size, 0, Math.PI * 2);
        ctx.fill();
      }
      ctx.shadowBlur = 0;
      ctx.lineCap = `butt`;
      ctx.globalCompositeOperation = `source-over`;
    }
    function drawPlanet(ctx, world, screen, now) {
      const hovered = starScene.hover === world;
      const radius = Math.max(16, world.radius * screen.scale);
      const pulse = hovered ? 1 + Math.sin(now * .006) * .035 : 1;
      const r = radius * pulse;
      ctx.save();
      ctx.translate(screen.x, screen.y);
      ctx.globalAlpha = world.enabled ? 1 : .48;
      ctx.strokeStyle = hovered ? world.accent : 'rgba(255,255,255,.16)';
      ctx.lineWidth = hovered ? 2.2 : 1.1;
      ctx.beginPath();
      ctx.ellipse(0, 0, r * 1.65, r * .48, -.32, 0, Math.PI * 2);
      ctx.stroke();

      const glow = ctx.createRadialGradient(-r * .35, -r * .35, 0, 0, 0, r * 1.6);
      glow.addColorStop(0, 'rgba(255,255,255,.92)');
      glow.addColorStop(.18, world.color);
      glow.addColorStop(.72, 'rgba(8,15,31,.92)');
      glow.addColorStop(1, 'rgba(0,0,0,.08)');
      ctx.fillStyle = glow;
      ctx.beginPath();
      ctx.arc(0, 0, r, 0, Math.PI * 2);
      ctx.fill();

      ctx.strokeStyle = hovered ? world.color : 'rgba(103,247,177,.22)';
      ctx.lineWidth = hovered ? 3 : 1;
      ctx.beginPath();
      ctx.arc(0, 0, r + (hovered ? 8 : 4), 0, Math.PI * 2);
      ctx.stroke();

      ctx.fillStyle = hovered ? '#eef5ff' : 'rgba(238,245,255,.70)';
      ctx.font = `${hovered ? 700 : 600} ${hovered ? 14 : 12}px ${getComputedStyle(document.body).fontFamily}`;
      ctx.textAlign = 'center';
      ctx.fillText(tagName(world), 0, -r - 18);
      if (!world.enabled) {
        ctx.fillStyle = 'rgba(170,184,208,.66)';
        ctx.font = `10px ${getComputedStyle(document.body).fontFamily}`;
        ctx.fillText(t('reserved'), 0, r + 18);
      }
      ctx.restore();
    }

    function drawPlanets(ctx, now) {
      const projected = tagWorlds
        .map(world => ({ world, screen: projectPoint(world.pos) }))
        .filter(item => item.screen)
        .sort((a, b) => b.screen.z - a.screen.z);
      for (const item of projected) drawPlanet(ctx, item.world, item.screen, now);
    }

    function settleCamera(now) {
      if (state.activeTab !== 'home' || starScene.drag || starScene.warp) return;
      if (now - starScene.lastInput < 3000) return;
      const cam = starScene.camera;
      const home = starScene.home;
      cam.rotX = lerp(cam.rotX, home.rotX, .025);
      cam.rotY = lerp(cam.rotY, home.rotY, .025);
      cam.zoom = lerp(cam.zoom, home.zoom, .025);
      cam.panX = lerp(cam.panX, home.panX, .025);
      cam.panY = lerp(cam.panY, home.panY, .025);
    }

    function renderStarMap(now) {
      starScene.frame = 0;
      if (!starScene.ctx) return;
      if (state.activeTab !== 'home' && !starScene.warp) return;
      if (starScene.warp) updateWarpCamera(now);
      else settleCamera(now);
      if (starScene.warp) starScene.hover = starScene.warp.world;
      else detectPlanetHover();
      const ctx = starScene.ctx;
      ctx.clearRect(0, 0, starScene.width, starScene.height);
      drawStarMapBackground(ctx, now);
      drawPlanets(ctx, now);
      starScene.frame = requestAnimationFrame(renderStarMap);
    }

    function startWarp(target) {
      const world = tagWorlds.find(item => item.id === target && item.enabled);
      if (!world || starScene.warp) return;
      resizeStarMap();
      const duration = starScene.reducedMotion ? 450 : 2000;
      const targetCamera = cameraForWorld(world);
      const now = performance.now();
      starScene.warp = { id: world.id, world, start: now, duration, from: cloneCamera(starScene.camera), target: targetCamera };
      starScene.hover = world;
      starScene.pointer.x = starScene.width / 2;
      starScene.pointer.y = starScene.height / 2;
      starScene.lastInput = now;
      updatePlanetHud(world);
      $('warpOverlay').classList.add('active');
      setTimeout(async () => {
        $('warpOverlay').classList.remove('active');
        starScene.warp = null;
        await switchTab(world.id);
      }, duration);
    }


    function phaseLabel(phase) {
      const value = String(phase || 'idle').toLowerCase();
      if (value === 'thinking') return t('phaseThinking');
      if (value === 'integrating') return t('phaseIntegrating');
      if (value === 'continuing') return t('phaseContinuing');
      if (value === 'tooling') return t('phaseTooling');
      if (value === 'idle') return t('idle');
      return phase;
    }

    function setBusy(busy, phase = busy ? 'thinking' : 'idle') {
      state.busy = busy;
      state.phase = phase;
      $('send').disabled = busy || state.sessionReadOnly;
      $('stopResponse').disabled = !busy;
      $('input').disabled = busy || state.sessionReadOnly;
      if (busy && state.voiceMode && !state.voiceRecording) state.voiceMode = false;
      updateVoiceUi();
      $('phase').textContent = phaseLabel(phase);
      if (busy) $('hint').textContent = t('busyHint');
      else renderAttachmentStrip();
      syncComposerState();
    }

    function syncComposerState() {
      const readOnly = !!state.sessionReadOnly;
      const busy = !!state.busy;
      if (readOnly && state.voiceMode) state.voiceMode = false;
      if (readOnly && (state.chatAttachments || []).length) clearChatAttachments();
      const composer = $('composer');
      if (composer) composer.classList.toggle('read-only', readOnly);
      const input = $('input');
      if (input) {
        input.disabled = busy || readOnly;
        input.placeholder = readOnly ? t('sessionReadOnlyPlaceholder') : t('inputPlaceholder');
      }
      const send = $('send');
      if (send) send.disabled = busy || readOnly;
      const attach = $('attachButton');
      if (attach) attach.disabled = busy || readOnly;
      const mic = $('micButton');
      if (mic) mic.disabled = busy || readOnly;
      updateVoiceUi();
    }

    const CHAT_BOTTOM_THRESHOLD = 82;
    const CHAT_JUMP_THRESHOLD = 180;
    let chatScrollFrame = 0;
    let chatScrollOptions = null;
    function chatBottomDistance(chat = $('chat')) {
      if (!chat) return 0;
      return Math.max(0, chat.scrollHeight - chat.clientHeight - chat.scrollTop);
    }
    function isChatNearBottom(chat = $('chat'), threshold = CHAT_BOTTOM_THRESHOLD) {
      return chatBottomDistance(chat) <= threshold;
    }
    function updateChatJumpButton() {
      const button = $('chatJumpBottom');
      const chat = $('chat');
      if (!button || !chat) return;
      const show = chatBottomDistance(chat) > CHAT_JUMP_THRESHOLD;
      button.hidden = !show;
      button.textContent = t('chatJumpBottom');
    }
    function setChatScrollTop(chat, top, ms = 260) {
      if (!chat) return;
      state.chatProgrammaticScrollUntil = Date.now() + Math.max(0, ms);
      chat.scrollTop = Math.max(0, top);
    }
    function scrollBottom(options = {}) {
      const chat = $('chat');
      if (!chat) return;
      const opts = typeof options === 'object' ? options : { force: !!options };
      if (state.suppressChatAutoScroll && !opts.force) {
        return;
      }
      const stream = opts.stream ?? state.busy;
      const shouldStick = !!opts.force || (stream ? state.chatFollowStream !== false : state.chatNearBottom !== false);
      if (!shouldStick) {
        state.chatNearBottom = isChatNearBottom(chat);
        updateChatJumpButton();
        return;
      }
      setChatScrollTop(chat, chat.scrollHeight);
      state.chatNearBottom = true;
      updateChatJumpButton();
    }
    function scheduleScrollBottom(options = {}) {
      const opts = typeof options === 'object' ? options : { force: !!options };
      chatScrollOptions = {
        ...(chatScrollOptions || {}),
        ...opts,
        force: !!(opts.force || chatScrollOptions?.force),
        stream: opts.stream ?? chatScrollOptions?.stream
      };
      if (chatScrollFrame) return;
      chatScrollFrame = requestAnimationFrame(function() {
        const next = chatScrollOptions || {};
        chatScrollFrame = 0;
        chatScrollOptions = null;
        scrollBottom(next);
      });
    }
    function clearChat() { $('chat').replaceChildren(); updateChatJumpButton(); }
    function noteChatUserScrollIntent(event) {
      state.chatUserScrollIntentAt = Date.now();
      if (state.busy && event && typeof event.deltaY === 'number' && event.deltaY < 0) {
        state.chatFollowStream = false;
      }
    }
    function handleChatScroll() {
      const chat = $('chat');
      if (!chat) return;
      const near = isChatNearBottom(chat);
      const userScroll = Date.now() > (state.chatProgrammaticScrollUntil || 0);
      state.chatNearBottom = near;
      if (state.busy && userScroll) state.chatFollowStream = near;
      updateChatJumpButton();
    }
    function jumpChatToBottom() {
      state.chatFollowStream = true;
      scrollBottom({ force: true });
    }
    function showEmpty(text) {
      clearChat();
      const node = document.createElement('div');
      node.className = 'empty';
      node.textContent = text;
      $('chat').appendChild(node);
    }

    function escapeHtml(value) {
      const map = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' };
      return String(value ?? '').replace(/[&<>"']/g, char => map[char]);
    }

    function renderInlineMarkdown(text) {
      let html = escapeHtml(text);
      html = html.replace(/!\[([^\]]*)\]\((https?:\/\/[^\s)]+)\)/g, '<a href="$2" target="_blank" rel="noreferrer">[image: $1]</a>');
      html = html.replace(/\[([^\]]+)\]\((https?:\/\/[^\s)]+)\)/g, '<a href="$2" target="_blank" rel="noreferrer">$1</a>');
      html = html.replace(/`([^`]+)`/g, '<code>$1</code>');
      html = html.replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>');
      html = html.replace(/__([^_]+)__/g, '<strong>$1</strong>');
      html = html.replace(/(^|[^*])\*([^*]+)\*/g, '$1<em>$2</em>');
      return html;
    }

    function renderMarkdown(value) {
      let source = String(value ?? '').replace(/\r\n/g, '\n');
      const blocks = [];
      source = source.replace(/```[^\n]*\n([\s\S]*?)```/g, (_, code) => {
        const token = `§CODE${blocks.length}§`;
        blocks.push(`<pre><code>${escapeHtml(code.trimEnd())}</code></pre>`);
        return `\n${token}\n`;
      });
      const blockTokens = new Map(blocks.map((_, index) => [`§CODE${index}§`, index]));
      const output = [];
      let inList = false;
      const closeList = () => { if (inList) { output.push('</ul>'); inList = false; } };
      for (const rawLine of source.split('\n')) {
        const line = rawLine.trimEnd();
        const trimmed = line.trim();
        if (!trimmed) { closeList(); continue; }
        const blockIndex = blockTokens.has(trimmed) ? blockTokens.get(trimmed) : -1;
        if (blockIndex >= 0) { closeList(); output.push(blocks[blockIndex]); continue; }
        const heading = trimmed.match(/^(#{1,3})\s+(.+)$/);
        if (heading) { closeList(); output.push(`<h${heading[1].length}>${renderInlineMarkdown(heading[2])}</h${heading[1].length}>`); continue; }
        const bullet = trimmed.match(/^(?:[-*]|\d+[.)])\s+(.+)$/);
        if (bullet) { if (!inList) { output.push('<ul>'); inList = true; } output.push(`<li>${renderInlineMarkdown(bullet[1])}</li>`); continue; }
        closeList();
        const quote = trimmed.match(/^>\s?(.+)$/);
        if (quote) { output.push(`<blockquote>${renderInlineMarkdown(quote[1])}</blockquote>`); continue; }
        output.push(`<p>${renderInlineMarkdown(line)}</p>`);
      }
      closeList();
      return output.join('');
    }

    function setMarkdownContent(node, content, parsePreviews = true) {
      const pieces = splitPlayAudioMarkerContent(content);
      if (pieces.some(piece => piece.type === 'cue')) {
        renderSoundCueGroupedContent(node, pieces, parsePreviews);
        return;
      }
      renderMarkdownContent(node, stripPlayAudioMarkers(content), parsePreviews);
    }

    function renderMarkdownContent(node, content, parsePreviews = true) {
      // Protect preview markers before markdown rendering
      const previews = [];
      let protectedContent = String(content || '').replace(/\[preview:([^\]]+)\]/g, (_, paths) => {
        const token = `§PREVIEW${previews.length}§`;
        previews.push(paths.trim());
        return token;
      });
      // Also support new {show_file:path} syntax
      protectedContent = protectedContent.replace(/\{show_file:([^}]+)\}/g, (_, path) => {
        const token = `§PREVIEW${previews.length}§`;
        previews.push(path.trim());
        return token;
      });

      // Render markdown
      node.innerHTML = renderMarkdown(protectedContent);

      // Replace preview tokens with actual preview boxes
      if (!parsePreviews) return;
      replaceTextTokensWithElements(node, previews.map((paths, index) => ({
        token: `§PREVIEW${index}§`,
        create: () => createPreviewBox(paths.split(',').map(p => p.trim()).filter(Boolean))
      })));
    }

    function renderSoundCueGroupedContent(node, pieces, parsePreviews = true) {
      node.innerHTML = '';
      let rootBuffer = '';
      let cueSegment = null;

      const flushRoot = function() {
        if (!rootBuffer) return;
        appendRenderedMarkdown(node, rootBuffer, parsePreviews);
        rootBuffer = '';
      };

      const flushCue = function() {
        if (!cueSegment) return;
        renderMarkdownContent(cueSegment.body, cueSegment.buffer, parsePreviews);
        cueSegment.body.hidden = !stripPlayAudioMarkers(cueSegment.buffer).trim();
      };

      pieces.forEach(function(piece) {
        if (piece.type === 'cue' && piece.cue) {
          flushRoot();
          flushCue();
          const block = createSoundCueEvent(piece.cue, 'reply', { played: true, static: true });
          node.appendChild(block);
          cueSegment = { block, body: soundCueEventBody(block), buffer: '' };
          return;
        }
        if (piece.type === 'text' && piece.text) {
          if (cueSegment) cueSegment.buffer += piece.text;
          else rootBuffer += piece.text;
        }
      });
      flushRoot();
      flushCue();
    }

    function appendRenderedMarkdown(parent, content, parsePreviews = true) {
      if (!content) return;
      const temp = document.createElement('div');
      renderMarkdownContent(temp, content, parsePreviews);
      while (temp.firstChild) parent.appendChild(temp.firstChild);
    }

    function replaceTextTokensWithElements(root, replacements) {
      const entries = (replacements || []).filter(entry => entry?.token && typeof entry.create === 'function');
      if (!entries.length) return 0;
      const nodes = [];
      const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT, null, false);
      let textNode;
      while (textNode = walker.nextNode()) {
        nodes.push(textNode);
      }

      let replaced = 0;
      nodes.forEach(function(node) {
        if (!node.parentNode) return;
        const text = node.textContent || '';
        const parent = node.parentElement;
        if (parent && parent.tagName === 'P') {
          const exact = entries.find(entry => parent.textContent.trim() === entry.token);
          if (exact) {
            parent.replaceWith(exact.create());
            replaced++;
            return;
          }
        }

        let cursor = 0;
        let fragment = null;
        while (cursor < text.length) {
          let found = null;
          let foundAt = -1;
          for (const entry of entries) {
            const index = text.indexOf(entry.token, cursor);
            if (index >= 0 && (foundAt < 0 || index < foundAt)) {
              found = entry;
              foundAt = index;
            }
          }
          if (!found) break;
          if (!fragment) fragment = document.createDocumentFragment();
          if (foundAt > cursor) fragment.appendChild(document.createTextNode(text.slice(cursor, foundAt)));
          fragment.appendChild(found.create());
          replaced++;
          cursor = foundAt + found.token.length;
        }

        if (fragment) {
          if (cursor < text.length) fragment.appendChild(document.createTextNode(text.slice(cursor)));
          node.replaceWith(fragment);
        }
      });
      return replaced;
    }

    function replaceTextTokenWithElement(root, token, element) {
      return replaceTextTokensWithElements(root, [{ token, create: () => element }]);
    }

    function createPreviewBox(paths) {
      const container = document.createElement('div');
      container.className = 'preview-box-container';

      paths.forEach(filePath => {
        const fullPath = resolvePreviewPath(filePath);
        const extension = (filePath.split('.').pop() || '').toLowerCase();
        const fileName = filePath.split(/[\\/]/).pop() || filePath;

        const fileType = extension.match(/^(png|jpg|jpeg|gif|webp|bmp|svg)$/) ? 'image' :
                         extension.match(/^(mp3|wav|ogg|oga|opus|m4a|aac|flac|webm)$/) ? 'audio' :
                         extension.match(/^(html|htm)$/) ? 'html' :
                         extension.match(/^(md|markdown)$/) ? 'markdown' :
                         extension.match(/^(txt|json|xml|csv|log|css|js|ts|py|ps1|sh|bat)$/) ? 'text' :
                         extension.match(/^(pdf|doc|docx|xls|xlsx|ppt|pptx)$/) ? 'document' :
                         'unknown';

        const box = document.createElement('div');
        box.className = 'preview-box preview-' + fileType;

        const titleBar = document.createElement('div');
        titleBar.className = 'preview-header';

        const fileMain = document.createElement('div');
        fileMain.className = 'preview-file-main';

        const icon = document.createElement('span');
        icon.className = 'preview-file-icon';
        icon.textContent = previewTypeIcon(fileType);

        const copy = document.createElement('div');
        copy.className = 'preview-file-copy';

        const name = document.createElement('span');
        name.className = 'preview-file-name';
        name.textContent = fileName;

        const meta = document.createElement('span');
        meta.className = 'preview-file-meta';
        meta.textContent = previewTypeLabel(fileType) + ' - ' + filePath;

        copy.append(name, meta);
        fileMain.append(icon, copy);

        const actions = document.createElement('div');
        actions.className = 'preview-actions';

        const maxBtn = document.createElement('button');
        maxBtn.type = 'button';
        maxBtn.className = 'preview-open-button';
        maxBtn.innerHTML = '\u2197'; // upper right arrow ↗
        maxBtn.title = t('previewOpenFull');
        maxBtn.textContent = '';
        maxBtn.title = t('previewOpenFull');
        maxBtn.setAttribute('aria-label', t('previewOpenFull'));

        actions.appendChild(maxBtn);
        titleBar.append(fileMain, actions);
        box.appendChild(titleBar);

        const body = document.createElement('div');
        body.className = 'preview-body';

        if (fileType === 'image') {
          const imgWrap = document.createElement('div');
          imgWrap.className = 'preview-surface';

          const loader = document.createElement('div');
          loader.className = 'preview-loader';
          loader.textContent = t('previewLoading');
          imgWrap.appendChild(loader);

          const img = document.createElement('img');
          img.src = previewFileUrl(fullPath);
          img.className = 'preview-image';
          img.onload = () => { loader.style.display = 'none'; };
          img.onerror = () => { loader.className = 'preview-loader error'; loader.textContent = t('previewLoadImageFailed'); };
          imgWrap.appendChild(img);
          body.appendChild(imgWrap);
          maxBtn.onclick = () => openFileBrowser(fullPath, fileName, 'image');
        } else if (fileType === 'audio') {
          const audioWrap = document.createElement('div');
          audioWrap.className = 'preview-surface';

          const audio = document.createElement('audio');
          audio.className = 'preview-audio';
          audio.controls = true;
          audio.src = previewFileUrl(fullPath);
          audioWrap.appendChild(audio);
          body.appendChild(audioWrap);
          maxBtn.onclick = () => openFileBrowser(fullPath, fileName, 'audio');
        } else if (fileType === 'html') {
          const iframeWrap = document.createElement('div');
          iframeWrap.className = 'preview-surface light';

          const loader = document.createElement('div');
          loader.className = 'preview-loader light';
          loader.textContent = t('previewLoading');
          iframeWrap.appendChild(loader);

          const iframe = document.createElement('iframe');
          iframe.className = 'preview-frame';
          iframe.onload = () => { loader.style.display = 'none'; };
          iframe.onerror = () => { loader.className = 'preview-loader light error'; loader.textContent = t('previewLoadFailed'); };
          const iframeUrl = previewFileUrl(fullPath);
          fetch(iframeUrl)
            .then(ensurePreviewResponse)
            .then(() => {
              iframe.src = iframeUrl;
              iframeWrap.appendChild(iframe);
            })
            .catch(err => {
              loader.className = 'preview-loader light error';
              loader.textContent = t('previewLoadFailed') + ': ' + err.message;
            });
          body.appendChild(iframeWrap);
          maxBtn.onclick = () => openFileBrowser(fullPath, fileName, 'html');
        } else if (fileType === 'markdown') {
          const mdWrap = document.createElement('div');
          mdWrap.className = 'preview-markdown-wrap';
          fetch(previewFileUrl(fullPath))
            .then(ensurePreviewResponse)
            .then(r => r.text())
            .then(text => {
              const md = document.createElement('div');
              md.className = 'markdown';
              setMarkdownContent(md, text);
              mdWrap.appendChild(md);
            })
            .catch(() => { mdWrap.innerHTML = '<div style="color:var(--faint);">' + escapeHtml(t('previewLoadMarkdownFailed')) + '</div>'; });
          body.appendChild(mdWrap);
          maxBtn.onclick = () => openFileBrowser(fullPath, fileName, 'markdown');
        } else if (fileType === 'text') {
          const preWrap = document.createElement('div');
          preWrap.className = 'preview-text-wrap';
          fetch(previewFileUrl(fullPath))
            .then(ensurePreviewResponse)
            .then(r => r.text())
            .then(text => {
              const pre = document.createElement('pre');
              pre.className = 'preview-code';
              const code = document.createElement('code');
              code.textContent = text.substring(0, 3000) + (text.length > 3000 ? '\n...[truncated]' : '');
              pre.appendChild(code);
              preWrap.appendChild(pre);
            })
            .catch(() => { preWrap.innerHTML = '<div style="color:var(--faint);">' + escapeHtml(t('previewLoadFileFailed')) + '</div>'; });
          body.appendChild(preWrap);
          maxBtn.onclick = () => openFileBrowser(fullPath, fileName, 'text');
        } else if (fileType === 'document') {
          body.innerHTML = `<div class="preview-placeholder">
            <div>${escapeHtml(t('previewDocument'))}</div>
            <a class="preview-link" href="${previewFileUrl(fullPath)}" target="_blank">${escapeHtml(t('previewOpenBrowser'))}</a>
          </div>`;
          maxBtn.onclick = () => openFileBrowser(fullPath, fileName, 'document');
        } else {
          body.innerHTML = `<div class="preview-placeholder">
            <div>${escapeHtml(t('previewUnavailable'))}</div>
            <a class="preview-link" href="${previewFileUrl(fullPath)}" target="_blank">${escapeHtml(t('previewDownload'))}</a>
          </div>`;
          maxBtn.onclick = () => openFileBrowser(fullPath, fileName, 'unknown');
        }

        box.appendChild(body);
        container.appendChild(box);
      });

      return container;
    }

    function resolvePreviewPath(filePath) {
      // Server resolves paths relative to workspace or browser_temp
      // Just return the path as-is for the API to resolve
      return filePath;
    }

    function previewFileUrl(filePath) {
      const params = new URLSearchParams();
      params.set('path', filePath);
      if (state.agent) params.set('agent', state.agent);
      return '/api/file?' + params.toString();
    }

    function ensurePreviewResponse(response) {
      if (!response.ok) throw new Error('HTTP ' + response.status);
      return response;
    }

    function previewTypeLabel(fileType) {
      return ({
        image: 'IMAGE',
        audio: 'AUDIO',
        html: 'HTML',
        markdown: 'MARKDOWN',
        text: 'TEXT',
        document: 'DOCUMENT',
        unknown: 'FILE'
      })[fileType] || 'FILE';
    }

    function previewTypeIcon(fileType) {
      return ({
        image: 'IMG',
        audio: 'AUD',
        html: '<>',
        markdown: 'MD',
        text: 'TXT',
        document: 'DOC',
        unknown: 'FILE'
      })[fileType] || 'FILE';
    }

    function openLightbox(src, type, fullPath) {
      const overlay = document.createElement('div');
      overlay.style.cssText = `
        position:fixed;inset:0;z-index:9999;
        background:rgba(0,0,0,.85);
        backdrop-filter:blur(12px);
        display:flex;align-items:center;justify-content:center;
        padding:40px;cursor:pointer;
        animation:fadeIn .2s ease;
      `;

      const content = document.createElement('div');
      content.style.cssText = `
        max-width:90vw;max-height:85vh;
        overflow:auto;border-radius:16px;
        box-shadow:0 25px 80px rgba(0,0,0,.6);
        cursor:default;
        animation:scaleIn .25s var(--ease-soft);
      `;

      if (type === 'image') {
        const img = document.createElement('img');
        img.src = src;
        img.style.cssText = 'max-width:100%;max-height:85vh;display:block;border-radius:16px;';
        content.appendChild(img);
      } else if (type === 'iframe') {
        const iframe = document.createElement('iframe');
        iframe.src = src;
        iframe.style.cssText = 'width:85vw;height:80vh;border:none;border-radius:16px;background:#fff;';
        content.appendChild(iframe);
      } else if (type === 'markdown' && fullPath) {
        fetch(previewFileUrl(fullPath))
          .then(ensurePreviewResponse)
          .then(r => r.text())
          .then(text => {
            const md = document.createElement('div');
            md.className = 'markdown';
            md.style.cssText = 'background:var(--bg1);padding:32px 40px;min-width:600px;max-width:800px;';
            setMarkdownContent(md, text);
            content.appendChild(md);
          });
      }

      overlay.appendChild(content);
      overlay.onclick = (e) => { if (e.target === overlay) overlay.remove(); };
      document.body.appendChild(overlay);

      // Add keyframe animations if not exists
      if (!document.getElementById('preview-animations')) {
        const style = document.createElement('style');
        style.id = 'preview-animations';
        style.textContent = `
          @keyframes fadeIn { from { opacity:0 } to { opacity:1 } }
          @keyframes scaleIn { from { opacity:0;transform:scale(.92) } to { opacity:1;transform:scale(1) } }
        `;
        document.head.appendChild(style);
      }
    }

    function openFileBrowser(filePath, fileName, fileType) {
      // Remove existing file browser
      const existing = document.getElementById('file-browser-overlay');
      if (existing) existing.remove();

      const overlay = document.createElement('div');
      overlay.id = 'file-browser-overlay';
      overlay.style.cssText = `
        position:fixed;inset:0;z-index:9998;
        background:rgba(5,7,17,.92);
        backdrop-filter:blur(14px);
        display:flex;align-items:center;justify-content:center;
        padding:28px;
        animation:fadeIn .22s ease;
      `;

      const window_ = document.createElement('div');
      window_.style.cssText = `
        width:min(94vw, 1440px);height:min(90vh, 940px);
        display:grid;grid-template-rows:auto 1fr;
        border:1px solid var(--line);
        border-radius:var(--radius-lg);
        background:var(--panel-solid);
        box-shadow:var(--shadow);
        overflow:hidden;
        animation:scaleIn .28s var(--ease-soft);
      `;

      // Title bar
      const titleBar = document.createElement('div');
      titleBar.style.cssText = `
        display:flex;align-items:center;justify-content:space-between;
        gap:12px;padding:12px 18px;
        border-bottom:1px solid var(--line);
        background:linear-gradient(180deg, rgba(255,255,255,.055), rgba(255,255,255,.025));
      `;

      const title = document.createElement('div');
      title.style.cssText = 'font-size:13px;color:var(--soft);display:flex;align-items:center;gap:10px;';
      const icon = fileType === 'image' ? '\u{1F5BC}' : fileType === 'audio' ? '\u{1F50A}' : fileType === 'html' ? '\u{1F310}' : fileType === 'markdown' ? '\u{1F4C4}' : fileType === 'document' ? '\u{1F4C2}' : '\u{1F4C1}';
      title.innerHTML = `<span style="font-size:16px;">${icon}</span><span style="font-weight:500;">${escapeHtml(fileName || filePath)}</span>`;

      const actions = document.createElement('div');
      actions.style.cssText = 'display:flex;align-items:center;gap:8px;';

      const openBtn = document.createElement('a');
      openBtn.href = previewFileUrl(filePath);
      openBtn.target = '_blank';
      openBtn.textContent = t('previewOpenRaw');
      openBtn.style.cssText = 'color:var(--cyan);font-size:12px;text-decoration:none;padding:4px 10px;border:1px solid rgba(100,219,255,.25);border-radius:8px;background:rgba(100,219,255,.06);';
      openBtn.onmouseenter = () => { openBtn.style.background = 'rgba(100,219,255,.12)'; };
      openBtn.onmouseleave = () => { openBtn.style.background = 'rgba(100,219,255,.06)'; };

      const closeBtn = document.createElement('button');
      closeBtn.type = 'button';
      closeBtn.innerHTML = '&times;';
      closeBtn.style.cssText = 'width:auto;min-height:28px;padding:0 10px;border-radius:8px;font-size:18px;color:var(--soft);background:rgba(255,255,255,.05);border:1px solid var(--line);cursor:pointer;';
      closeBtn.onclick = () => overlay.remove();

      actions.append(openBtn, closeBtn);
      titleBar.append(title, actions);

      // Content area
      const contentArea = document.createElement('div');
      contentArea.style.cssText = 'position:relative;overflow:hidden;background:#000;';

      const fileUrl = previewFileUrl(filePath);

      if (fileType === 'image') {
        const imgWrap = document.createElement('div');
        imgWrap.style.cssText = 'width:100%;height:100%;display:flex;align-items:center;justify-content:center;overflow:auto;padding:20px;';
        const img = document.createElement('img');
        img.src = fileUrl;
        img.style.cssText = 'max-width:100%;max-height:100%;object-fit:contain;border-radius:8px;box-shadow:0 8px 32px rgba(0,0,0,.4);';
        imgWrap.appendChild(img);
        contentArea.appendChild(imgWrap);
      } else if (fileType === 'audio') {
        const audioWrap = document.createElement('div');
        audioWrap.style.cssText = 'width:100%;height:100%;display:flex;align-items:center;justify-content:center;padding:28px;background:var(--bg1);';
        const audio = document.createElement('audio');
        audio.controls = true;
        audio.src = fileUrl;
        audio.style.cssText = 'width:min(720px,100%);';
        audioWrap.appendChild(audio);
        contentArea.appendChild(audioWrap);
      } else if (fileType === 'html') {
        const iframe = document.createElement('iframe');
        iframe.src = fileUrl;
        iframe.style.cssText = 'width:100%;height:100%;border:none;background:#fff;';
        contentArea.appendChild(iframe);
      } else if (fileType === 'markdown') {
        const mdWrap = document.createElement('div');
        mdWrap.style.cssText = 'width:100%;height:100%;overflow:auto;padding:28px 36px;background:var(--bg1);';
        fetch(fileUrl)
          .then(ensurePreviewResponse)
          .then(r => r.text())
          .then(text => {
            const md = document.createElement('div');
            md.className = 'markdown';
            md.style.cssText = 'max-width:860px;margin:0 auto;';
            setMarkdownContent(md, text);
            mdWrap.appendChild(md);
          })
          .catch(() => { mdWrap.innerHTML = '<div style="color:var(--faint);text-align:center;padding:40px;">' + escapeHtml(t('previewLoadMarkdownFailed')) + '</div>'; });
        contentArea.appendChild(mdWrap);
      } else if (fileType === 'text') {
        const preWrap = document.createElement('div');
        preWrap.style.cssText = 'width:100%;height:100%;overflow:auto;padding:24px;background:var(--bg1);';
        fetch(fileUrl)
          .then(ensurePreviewResponse)
          .then(r => r.text())
          .then(text => {
            const pre = document.createElement('pre');
            pre.style.cssText = 'margin:0;background:rgba(0,0,0,.35);border-radius:12px;padding:20px;overflow:auto;font-size:13px;line-height:1.65;color:var(--soft);';
            const code = document.createElement('code');
            code.textContent = text;
            pre.appendChild(code);
            preWrap.appendChild(pre);
          })
          .catch(() => { preWrap.innerHTML = '<div style="color:var(--faint);text-align:center;padding:40px;">' + escapeHtml(t('previewLoadFileFailed')) + '</div>'; });
        contentArea.appendChild(preWrap);
      } else if (fileType === 'document') {
        // For PDF and Office docs, let the browser handle it via iframe
        const iframe = document.createElement('iframe');
        iframe.src = fileUrl;
        iframe.style.cssText = 'width:100%;height:100%;border:none;background:#fff;';
        contentArea.appendChild(iframe);
      } else {
        const placeholder = document.createElement('div');
        placeholder.style.cssText = 'width:100%;height:100%;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:16px;color:var(--soft);';
        placeholder.innerHTML = `
          <div style="font-size:48px;opacity:.4;">\u{1F4C1}</div>
          <div style="font-size:14px;">${escapeHtml(t('previewUnavailableInline'))}</div>
          <a href="${fileUrl}" target="_blank" style="color:var(--cyan);font-size:13px;text-decoration:none;padding:8px 16px;border:1px solid rgba(100,219,255,.25);border-radius:10px;background:rgba(100,219,255,.06);">${escapeHtml(t('previewDownload'))}</a>
        `;
        contentArea.appendChild(placeholder);
      }

      window_.append(titleBar, contentArea);
      overlay.appendChild(window_);

      // Close on backdrop click
      overlay.onclick = (e) => { if (e.target === overlay) overlay.remove(); };
      // Close on Escape key
      const onKey = (e) => { if (e.key === 'Escape') { overlay.remove(); document.removeEventListener('keydown', onKey); } };
      document.addEventListener('keydown', onKey);

      document.body.appendChild(overlay);
    }

    function truncateText(value, max = 10) {
      const text = String(value || '').replace(/\s+/g, ' ').trim();
      if (!text) return t('notFilledStep');
      return text.length > max ? `${text.slice(0, max)}…` : text;
    }

    function normalizeTaskStatus(status) {
      const value = String(status || 'pending').toLowerCase();
      if (['done', 'complete', 'completed', 'success', 'succeeded'].includes(value)) return 'done';
      if (['in_process', 'running', 'active', 'doing'].includes(value)) return 'running';
      return 'pending';
    }

    function taskStatusIcon(status) {
      const normalized = normalizeTaskStatus(status);
      if (normalized === 'done') return '✓';
      if (normalized === 'running') return '';
      return '•';
    }


    function currentAgentInfo(agentName = state.agent) {
      return state.agents.find(function(agent) { return agent.name === agentName; }) ?? null;
    }

    function agentDisplayName(agentName = state.agent) {
      const info = currentAgentInfo(agentName);
      let name = agentName ?? 'Agent';
      if (info) name = info.displayName ?? info.name ?? name;
      return name;
    }

    function agentInitial(name) {
      const chars = Array.from(String(name ?? 'A').trim());
      return (chars[0] ?? 'A').toUpperCase();
    }

    function assistantMeta(suffix = '') {
      const name = agentDisplayName();
      return suffix ? name + ' · ' + suffix : name;
    }

    function applyAvatar(node, role) {
      node.innerHTML = '';
      if (role === 'user') { node.textContent = agentInitial(t('you')); return; }
      if (role === 'tool') { node.textContent = 'T'; return; }
      const info = currentAgentInfo();
      const name = agentDisplayName();
      if (info) {
        if (info.iconUrl) {
          const img = document.createElement('img');
          img.src = info.iconUrl;
          img.alt = name;
          node.appendChild(img);
          return;
        }
      }
      node.textContent = agentInitial(name);
    }

    function setNodeText(selector, text) {
      const node = document.querySelector(selector);
      if (node) node.textContent = text;
    }

    function setText(id, key) {
      const node = $(id);
      if (node) node.textContent = t(key);
    }

    function setAllText(selector, key) {
      document.querySelectorAll(selector).forEach(function(node) { node.textContent = t(key); });
    }

    function setLabel(forId, mainKey, subKey) {
      const label = document.querySelector('label[for="' + forId + '"]');
      if (!label) return;
      label.innerHTML = '<span>' + escapeHtml(t(mainKey)) + '</span><span>' + escapeHtml(t(subKey)) + '</span>';
    }

    function setAttr(id, attr, key) {
      const node = $(id);
      if (node) node.setAttribute(attr, t(key));
    }

    function setPlaceholder(id, key) {
      const node = $(id);
      if (node) node.setAttribute('placeholder', t(key));
    }

    function setCssStringVar(name, value) {
      document.documentElement.style.setProperty(name, JSON.stringify(value));
    }

    function applyI18n() {
      const zh = state.lang === 'zh';
      document.documentElement.lang = zh ? 'zh-CN' : 'en';
      ['send','stopResponse','newSession','refresh','agentReload','agentFormReload','agentSaveTop','agentSave','agentCreate','agentDelete','agentIconUpload','langToggle','chatJumpBottom','settingsSideTitle','settingsSideText','settingsReserveTitle','settingsReserveLanguage','settingsReserveTheme','settingsReserveShortcuts','settingsReserveAbout','settingsTitle','settingsSubtitle','settingsModeLabel','settingsNavGeneralTitle','settingsNavGeneralSub','settingsNavMemoryTitle','settingsNavMemorySub','settingsNavSoundTitle','settingsNavSoundSub','settingsNavMultiTitle','settingsNavMultiSub','settingsGeneralTitle','settingsGeneralDesc','settingsGeneralState','runtimeEventsTitle','runtimeEventsDesc','runtimeEventsReload','languageTitle','languageDescription','privacyAccessTitle','privacyAccessDesc','privacyAccessToggleLabel','skillValidationTitle','skillValidationDesc','skillValidationEnabledLabel','skillValidationIntervalLabel','skillValidationIntervalMeta','skillValidationBatchLabel','skillValidationBatchMeta','soundCueTitle','soundCueDesc','soundCueEnabledLabel','soundCueVolumeLabel','soundCueDelayLabel','soundCueLibraryTitle','soundCueLibraryDesc','soundCueImport','soundCueExport','ttsErrorTitle','blankTitle','blankText','blankBack'].forEach(function(id) { setText(id, id === 'send' ? 'send' : id); });
      setText('agentSaveTop', 'save');
      setText('agentSave', 'saveConfig');
      setText('agentReload', 'reload');
      setText('agentFormReload', 'reload');
      setText('agentCreate', 'newAgent');
      setText('agentDelete', 'deleteAgent');
      setText('agentIconUpload', 'uploadAvatar');
      const input = $('input');
      if (input) input.placeholder = t('inputPlaceholder');
      const apiKey = $('configApiKey');
      if (apiKey) apiKey.placeholder = t('apiKeyPlaceholder');
      const apiKeyLink = $('configApiKeyLink');
      if (apiKeyLink) apiKeyLink.textContent = t('apiKeyLink');
      const modelToggle = $('configModelToggle');
      if (modelToggle) modelToggle.setAttribute('aria-label', t('modelDropdownLabel'));
      setAttr('attachButton', 'title', 'attachFiles');
      setAttr('attachButton', 'aria-label', 'attachFiles');
      if (!state.busy) { renderAttachmentStrip(); $('phase').textContent = t('idle'); }
      else $('phase').textContent = phaseLabel(state.phase);
      syncComposerState();
      setNodeText('.home-subtitle', t('homeSubtitle'));
      setNodeText('.home-eyebrow', t('homeEyebrow'));
      const controls = document.querySelectorAll('.control-readout span');
      ['homeCtrlRotate','homeCtrlZoom','homeCtrlPan','homeCtrlReset'].forEach(function(key, index) { if (controls[index]) controls[index].textContent = t(key); });
      setCssStringVar('--lab-empty-text', t('labNoResult'));
      const chips = document.querySelectorAll('.planet-dock .planet-chip');
      ['tagChatName','tagAgentName','tagScheduleName','tagSkillsName','tagLabName','tagSettingsName','reservedWorkspace','tagMemoryName'].forEach(function(key, index) { if (chips[index]) chips[index].textContent = t(key); });
      setNodeText('.brand-top p', t('brandSubtitle'));
      const brandChips = document.querySelectorAll('.brand-sub .chip');
      ['chipStreaming','chipTools','chipMemory'].forEach(function(key, index) { if (brandChips[index]) brandChips[index].textContent = t(key); });
      setLabel('agentSelect', 'agentLabel', 'profileLabel');
      setLabel('sessionSelect', 'sessionLabel', 'timelineLabel');
      setLabel('runtimeEventsAgentSelect', 'runtimeEventsAgentLabel', 'runtimeEventsAgentMeta');
      const metricLabels = document.querySelectorAll('.metric-card .metric-line span');
      ['metricModel','metricContext','metricMessages','metricToolCalls'].forEach(function(key, index) { if (metricLabels[index]) metricLabels[index].textContent = t(key); });
      const note = document.querySelector('.note-card');
      if (note) note.innerHTML = '<strong>' + escapeHtml(t('noteTitle')) + '</strong><br />' + escapeHtml(t('noteText'));
      if (!state.session) { $('title').textContent = t('titleReady'); $('subtitle').textContent = t('subtitleReady'); }
      setNodeText('.agent-panel .agent-hero h2', t('agentCenterTitle'));
      setNodeText('.agent-panel .agent-hero p', t('agentCenterText'));
      setLabel('agentConfigSelect', 'agentLabel', 'configTargetLabel');
      const statusLabels = document.querySelectorAll('.agent-status-card span');
      ['agentStatusSessions','agentStatusApiKey','agentStatusHotMemory','agentStatusCoreMemory'].forEach(function(key, index) { if (statusLabels[index]) statusLabels[index].textContent = t(key); });
      setNodeText('.agent-panel .reserve-title', t('reservedTabs'));
      const reserveChips = document.querySelectorAll('.agent-panel .reserve-chip');
      ['reservedWorkspace','reservedMemory','reservedTools','reservedRuns','reservedSettings'].forEach(function(key, index) { if (reserveChips[index]) reserveChips[index].textContent = t(key); });
      setNodeText('.agent-panel .agent-main-head h2', t('agentConfigTitle'));
      setNodeText('.agent-panel .phase-pill span:last-child', t('localConfig'));
      if ($('agentConfigState') && !$('agentConfigState').dataset.dynamic) $('agentConfigState').textContent = t('agentConfigInitial');
      setLabel('configName', 'nameLabel', 'folderIdLabel');
      setNodeText('label[for="configName"] + input + small', t('identityHelp'));
      setLabel('configApiType', 'apiTypeLabel', 'providerLabel');
      setNodeText('#apiTypeHelp', t('apiTypeHelp'));
      setLabel('configBaseUrl', 'baseUrlLabel', 'endpointLabel');
      setLabel('configModelId', 'modelLabel', 'modelIdLabel');
      setLabel('configApiKey', 'apiKeyLabel', 'hiddenOnLoadLabel');
      setLabel('configContextWindow', 'contextLabel', 'tokensLabel');
      setLabel('configMaxOutputToken', 'maxOutputLabel', 'tokensLabel');
      setLabel('configMaxConcurrency', 'maxConcurrencyLabel', 'backgroundSlotsLabel');
      setLabel('configTemperature', 'temperatureLabel', 'temperatureRangeLabel');
      const pathLabels = document.querySelectorAll('.agent-paths .path-row span');
      ['pathConfig','pathWorkspace','pathMemory','pathIcons'].forEach(function(key, index) { if (pathLabels[index]) pathLabels[index].textContent = t(key); });
      ['winClose','winMin','agentWinClose','agentWinMin','settingsWinClose','settingsWinMin','scheduleWinClose','scheduleWinMin','skillsWinClose','skillsWinMin','labWinClose','labWinMin','memoryWinClose','memoryWinMin'].forEach(function(id) { setAttr(id, 'title', 'backToHomeTitle'); });
      ['winMax','agentWinMax','settingsWinMax','scheduleWinMax','skillsWinMax','labWinMax','memoryWinMax'].forEach(function(id) { setAttr(id, 'title', 'fullscreenTitle'); });
      if ($('agentAvatarPreview')) renderAgentAvatarPreview({ agent: state.agent ?? 'Agent', config: currentAgentInfo() ?? {} });
      updatePlanetHud(starScene.hover);
      if (!$('commandMenu')?.hidden) renderCommandMenu();
      updateScheduleLang();
      updateSkillsLang();
      updateMemoryLang();
      setNodeText('#memoryLimitTitle', t('memoryLimitTitle'));
      setNodeText('#memoryLimitDesc', t('memoryLimitDesc'));
      setNodeText('#settingsHotLabel', t('memoryLimitHot'));
      setNodeText('#settingsCoreLabel', t('memoryLimitCore'));
      setNodeText('#settingsUserLabel', t('memoryLimitUser'));
      setNodeText('#settingsIdentityLabel', t('memoryLimitIdentity'));
      setText('settingsSaveLimits', 'memoryLimitSave');
      updateSettingsSection();
      renderSecuritySettings();
      renderSkillValidationSettings();
      renderSoundCueSettings();
      updateMultiModalLang();
      updateLabLang();
      updateVoiceUi();
    }
    function updateMultiModalLang() {
      setNodeText('#multiTitle', t('multiTitle'));
      setNodeText('#multiDescription', t('multiDescription'));
      setNodeText('#multiGlobalTitle', t('multiGlobalTitle'));
      setText('multiReload', 'reload');
      setText('multiSave', 'multiSave');
      if (state.multimodal) renderMultiModalSettings();
    }

    function switchSettingsSection(section) {
      const target = ['general', 'memory', 'sound', 'multimodal'].includes(section) ? section : 'general';
      state.settingsSection = target;
      updateSettingsSection();
    }

    function updateSettingsSection() {
      const current = state.settingsSection || 'general';
      document.querySelectorAll('[data-settings-section]').forEach(function(button) {
        button.classList.toggle('active', button.dataset.settingsSection === current);
      });
      document.querySelectorAll('[data-settings-section-view]').forEach(function(view) {
        view.classList.toggle('active', view.dataset.settingsSectionView === current);
      });
      const sectionTitleKey = current === 'memory' ? 'settingsNavMemoryTitle' : current === 'sound' ? 'settingsNavSoundTitle' : current === 'multimodal' ? 'settingsNavMultiTitle' : 'settingsNavGeneralTitle';
      const sectionSubKey = current === 'memory' ? 'settingsNavMemorySub' : current === 'sound' ? 'settingsNavSoundSub' : current === 'multimodal' ? 'settingsNavMultiSub' : 'settingsNavGeneralSub';
      if ($('settingsTitle')) $('settingsTitle').textContent = t(sectionTitleKey);
      if ($('settingsSubtitle')) $('settingsSubtitle').textContent = t(sectionSubKey);
      if (current === 'general') {
        loadRuntimeEvents().catch(function() {});
        startRuntimeEventsSync();
      } else {
        stopRuntimeEventsSync();
      }
    }

    async function loadSecuritySettings() {
      try {
        state.securitySettings = await api('/api/security-settings');
      } catch {
        state.securitySettings = { allowPrivateDataAccess: false };
      }
      renderSecuritySettings();
    }

    function renderSecuritySettings() {
      const settings = state.securitySettings || { allowPrivateDataAccess: false };
      const enabled = !!settings.allowPrivateDataAccess;
      const toggle = $('privacyAccessToggle');
      if (toggle) toggle.checked = enabled;
      const stateNode = $('privacyAccessState');
      if (stateNode) stateNode.textContent = enabled ? t('privacyAccessOn') : t('privacyAccessOff');
    }

    async function saveSecuritySettingsFromUi() {
      const enabled = !!$('privacyAccessToggle')?.checked;
      try {
        state.securitySettings = await api('/api/security-settings', {
          method: 'POST',
          headers: { 'content-type': 'application/json' },
          body: JSON.stringify({ allowPrivateDataAccess: enabled })
        });
      } catch (err) {
        $('hint').textContent = t('privacyAccessSaveFailed') + ': ' + (err.message || String(err));
      }
      renderSecuritySettings();
    }

    function defaultSkillValidationSettings() {
      return { autoSkillValidationEnabled: true, autoSkillValidationIntervalHours: 6, autoSkillValidationBatchSize: 1 };
    }

    function normalizeSkillValidationSettings(settings) {
      const defaults = defaultSkillValidationSettings();
      const source = settings || defaults;
      return {
        autoSkillValidationEnabled: source.autoSkillValidationEnabled !== false,
        autoSkillValidationIntervalHours: Math.max(1, Math.min(168, parseInt(source.autoSkillValidationIntervalHours || defaults.autoSkillValidationIntervalHours, 10) || defaults.autoSkillValidationIntervalHours)),
        autoSkillValidationBatchSize: Math.max(1, Math.min(3, parseInt(source.autoSkillValidationBatchSize || defaults.autoSkillValidationBatchSize, 10) || defaults.autoSkillValidationBatchSize)),
        updatedAt: source.updatedAt || null
      };
    }

    async function loadSkillValidationSettings() {
      try {
        state.skillValidationSettings = normalizeSkillValidationSettings(await api('/api/skill-validation-settings'));
      } catch {
        state.skillValidationSettings = defaultSkillValidationSettings();
      }
      renderSkillValidationSettings();
    }

    function renderSkillValidationSettings() {
      const settings = normalizeSkillValidationSettings(state.skillValidationSettings);
      const enabled = !!settings.autoSkillValidationEnabled;
      const enabledNode = $('skillValidationEnabled');
      if (enabledNode) enabledNode.checked = enabled;
      const intervalNode = $('skillValidationIntervalHours');
      if (intervalNode) intervalNode.value = settings.autoSkillValidationIntervalHours;
      const batchNode = $('skillValidationBatchSize');
      if (batchNode) batchNode.value = String(settings.autoSkillValidationBatchSize);
      const stateNode = $('skillValidationState');
      if (stateNode) {
        stateNode.textContent = enabled
          ? t('skillValidationStateText')
              .replace('{hours}', settings.autoSkillValidationIntervalHours)
              .replace('{count}', settings.autoSkillValidationBatchSize)
          : t('skillValidationDisabledState');
      }
    }

    async function saveSkillValidationSettingsFromUi() {
      const normalized = normalizeSkillValidationSettings({
        autoSkillValidationEnabled: !!$('skillValidationEnabled')?.checked,
        autoSkillValidationIntervalHours: $('skillValidationIntervalHours')?.value,
        autoSkillValidationBatchSize: $('skillValidationBatchSize')?.value
      });
      const payload = {
        autoSkillValidationEnabled: normalized.autoSkillValidationEnabled,
        autoSkillValidationIntervalHours: normalized.autoSkillValidationIntervalHours,
        autoSkillValidationBatchSize: normalized.autoSkillValidationBatchSize
      };
      try {
        state.skillValidationSettings = normalizeSkillValidationSettings(await api('/api/skill-validation-settings', {
          method: 'POST',
          headers: { 'content-type': 'application/json' },
          body: JSON.stringify(payload)
        }));
      } catch (err) {
        $('hint').textContent = t('skillValidationSaveFailed') + ': ' + (err.message || String(err));
      }
      renderSkillValidationSettings();
    }

    function startRuntimeEventsSync() {
      stopRuntimeEventsSync();
      if (state.activeTab !== 'settings' || state.settingsSection !== 'general') return;
      state.runtimeEventsTimer = setInterval(function() {
        if (state.activeTab === 'settings' && state.settingsSection === 'general') {
          loadRuntimeEvents().catch(function() {});
        }
      }, 10000);
    }

    function stopRuntimeEventsSync() {
      if (state.runtimeEventsTimer) {
        clearInterval(state.runtimeEventsTimer);
        state.runtimeEventsTimer = null;
      }
    }

    async function loadRuntimeEvents() {
      const list = $('runtimeEventsList');
      if (!list) return;
      const agent = state.runtimeEventsAgent || state.agent;
      if (!agent) {
        renderRuntimeEvents({ events: [], summary: {}, remaining: [] });
        return;
      }
      const data = await api('/api/runtime-events?agent=' + encodeURIComponent(agent) + '&take=80');
      renderRuntimeEvents(data);
    }
    function renderRuntimeEvents(data) {
      const list = $('runtimeEventsList');
      if (!list) return;
      const events = Array.isArray(data) ? data : (data?.events || []);
      const summary = Array.isArray(data) ? {} : (data?.summary || {});
      const remaining = Array.isArray(data) ? [] : (data?.remaining || []);
      renderRuntimeEventSummary(summary);
      renderRuntimeRemaining(remaining);
      list.innerHTML = '';
      if (!events.length) {
        const empty = document.createElement('div');
        empty.className = 'task-empty';
        empty.textContent = t('runtimeEventsEmpty');
        list.appendChild(empty);
        return;
      }
      for (const event of events) {
        const card = document.createElement('div');
        card.className = 'memory-item';
        const header = document.createElement('div');
        header.className = 'memory-item-header';
        const title = document.createElement('span');
        title.className = 'memory-item-id';
        title.textContent = (event.category || 'runtime') + ' / ' + (event.kind || event.jobId || '');
        const time = document.createElement('span');
        time.className = 'memory-item-time';
        time.textContent = event.timestamp ? new Date(event.timestamp).toLocaleString() : '';
        header.append(title, time);
        const content = document.createElement('div');
        content.className = 'memory-item-content';
        const advice = event.adviceKey ? t('runtimeAdvice_' + event.adviceKey) : '';
        content.textContent = '[' + runtimeStatusLabel(event.status) + '] ' + (event.message || '') + (advice ? '\n' + advice : '');
        card.append(header, content);
        list.appendChild(card);
      }
    }

    function renderRuntimeEventSummary(summary) {
      const box = $('runtimeEventsSummary');
      if (!box) return;
      box.innerHTML = '';
      const items = [
        ['runtimeEventsCompleted', summary?.completed || 0],
        ['runtimeEventsUnfinished', summary?.unfinished || 0],
        ['runtimeEventsSkipped', summary?.skipped || 0],
        ['runtimeEventsFailed', summary?.failed || 0],
        ['runtimeEventsRemaining', summary?.remaining || 0]
      ];
      for (const item of items) {
        const row = document.createElement('span');
        const label = document.createElement('span');
        label.textContent = t(item[0]);
        const value = document.createElement('b');
        value.textContent = String(item[1]);
        row.append(label, value);
        box.appendChild(row);
      }
    }

    function renderRuntimeRemaining(remaining) {
      const box = $('runtimeEventsRemaining');
      if (!box) return;
      box.innerHTML = '';
      if (!remaining.length) return;
      const title = document.createElement('div');
      title.className = 'memory-item';
      const titleContent = document.createElement('div');
      titleContent.className = 'memory-item-content';
      titleContent.textContent = t('runtimeEventsRemainingTitle');
      title.appendChild(titleContent);
      box.appendChild(title);
      for (const event of remaining.slice(0, 5)) {
        const card = document.createElement('div');
        card.className = 'memory-item';
        const header = document.createElement('div');
        header.className = 'memory-item-header';
        const name = document.createElement('span');
        name.className = 'memory-item-id';
        name.textContent = (event.category || 'runtime') + ' / ' + (event.kind || event.jobId || '');
        const status = document.createElement('span');
        status.className = 'memory-item-time';
        status.textContent = runtimeStatusLabel(event.status);
        header.append(name, status);
        const content = document.createElement('div');
        content.className = 'memory-item-content';
        content.textContent = event.message || '';
        card.append(header, content);
        box.appendChild(card);
      }
    }

    function runtimeStatusLabel(status) {
      const normalized = String(status || '').trim().toLowerCase().replace(/_/g, '-');
      if (['completed','complete','succeeded','success','done','delivered'].includes(normalized)) return t('runtimeStatusCompleted');
      if (['skipped','skip','no-op','noop'].includes(normalized)) return t('runtimeStatusSkipped');
      if (['failed','failure','error','errored','canceled','cancelled','rejected'].includes(normalized)) return t('runtimeStatusFailed');
      return t('runtimeStatusUnfinished');
    }

    function updateLabLang() {
      setNodeText('#labHeroTitle', t('labHeroTitle'));
      setNodeText('#labHeroText', t('labHeroText'));
      setNodeText('#labAgentLabel', t('labAgentLabel'));
      setText('labReload', 'reload');
      setNodeText('#labNoteTitle', t('labNoteTitle'));
      setNodeText('#labNoteText', t('labNoteText'));
      setNodeText('#labMainTitle', t('labMainTitle'));
      setNodeText('#labPhaseLabel', t('labPhaseLabel'));
      setNodeText('#labImageTitle', t('labImageTitle'));
      setNodeText('#labTtsTitle', t('labTtsTitle'));
      setNodeText('#labSttTitle', t('labSttTitle'));
      setText('labImageRun', 'labImageRun');
      setText('labTtsRun', 'labTtsRun');
      setText('labSttRun', 'labSttRun');
      setText('labSttRecord', state.labRecorder ? 'stopResponse' : 'labSttRecord');
      setPlaceholder('labImagePrompt', 'labImagePlaceholder');
      setPlaceholder('labTtsText', 'labTtsPlaceholder');
      setPlaceholder('labTtsVoice', 'labTtsVoicePlaceholder');
      const stateNode = $('labState');
      if (stateNode && !stateNode.dataset.dynamic) stateNode.textContent = t('labState');
    }
    function updateScheduleLang() {
      setNodeText('#scheduleHeroTitle', t('scheduleTitle'));
      setNodeText('#scheduleHeroText', t('scheduleSubtitle'));
      setCssStringVar('--schedule-queue-label', t('scheduleQueueLabel'));
      setCssStringVar('--schedule-trace-label', t('scheduleTraceLabel'));
      setCssStringVar('--schedule-task-spec-label', t('scheduleTaskSpecLabel'));
      setNodeText('#scheduleAgentLabel', t('scheduleAgentLabel'));
      setText('scheduleReload', 'scheduleReload');
      setNodeText('#scheduleListTitle', t('scheduleListTitle'));
      setText('schedulePrev', 'schedulePrev');
      setText('scheduleNext', 'scheduleNext');
      setNodeText('#scheduleRuleTitle', t('scheduleDeliveryRule'));
      setNodeText('#scheduleRuleText', t('scheduleDeliveryRuleText'));
      setNodeText('#scheduleMainTitle', t('scheduleTitle'));
      setNodeText('#scheduleTab .phase-pill span:last-child', t('schedulePhaseLabel'));
      const scheduleState = $('scheduleState');
      if (scheduleState && !scheduleState.dataset.dynamic && !state.scheduledTasks) scheduleState.textContent = t('scheduleStateInit');
      setLabel('scheduleTitle', 'scheduleTitleLabel', 'title');
      setLabel('scheduleStatus', 'scheduleStatusLabel', 'status');
      const enabledOption = document.querySelector('#scheduleStatus option[value="enabled"]');
      const pausedOption = document.querySelector('#scheduleStatus option[value="paused"]');
      if (enabledOption) enabledOption.textContent = t('scheduleStatusEnabled');
      if (pausedOption) pausedOption.textContent = t('scheduleStatusPaused');
      setLabel('scheduleContent', 'scheduleContentLabel', 'task');
      setPlaceholder('scheduleContent', 'scheduleContentPlaceholder');
      setNodeText('#scheduleLabelRule', t('scheduleScheduleLabel'));
      setText('scheduleRuleDaily', 'scheduleRuleDaily');
      setText('scheduleRuleDailyTimes', 'scheduleRuleDailyTimes');
      setText('scheduleRuleDailyWindow', 'scheduleRuleDailyWindow');
      setText('scheduleRuleOnce', 'scheduleRuleOnce');
      setLabel('scheduleTargetMode', 'scheduleTargetsLabel', 'targets');
      setText('scheduleOptCreated', 'scheduleTargetCreated');
      setText('scheduleOptSession', 'scheduleTargetSession');
      setText('scheduleOptAll', 'scheduleTargetAll');
      setText('scheduleOptNotification', 'scheduleTargetNotification');
      setText('scheduleOptNone', 'scheduleTargetNone');
      setLabel('scheduleTargetSessions', 'scheduleSessionsLabel', 'multi');
      setText('scheduleSave', 'scheduleSaveTask');
      setText('scheduleNew', 'scheduleNewTask');
      setNodeText('#scheduleHistoryTitle', t('scheduleHistoryTitle'));
      const historyPlaceholder = $('scheduleHistoryPlaceholder');
      if (historyPlaceholder) historyPlaceholder.textContent = t('scheduleHistoryPlaceholder');
      // Update rule editor "add" button if visible
      const addBtn = $('ruleAddTime');
      if (addBtn) addBtn.textContent = t('scheduleRuleAdd');
      // Refresh list to update labels
      if (state.scheduledTasks) renderScheduledTasks(state.scheduledTasks);
    }
    function updateSkillsLang() {
      setCssStringVar('--skills-editor-label', t('skillsEditorLabel'));
      setCssStringVar('--skills-library-label', t('skillsLibraryLabel'));
      setCssStringVar('--skills-preview-label', t('skillsPreviewLabel'));
      setNodeText('#skillsHeroTitle', t('skillsSideTitle'));
      setNodeText('#skillsHeroText', t('skillsSideText'));
      setNodeText('#skillsAgentLabel', t('skillsAgentLabel'));
      setText('skillsReload', 'skillsReload');
      setText('skillsOrganize', 'skillsOrganize');
      setText('skillsLearnValidate', 'skillsLearnValidate');
      setNodeText('#skillsLearnTitle', t('skillsLearnTitle'));
      setNodeText('#skillsLearnDesc', t('skillsLearnDesc'));
      setNodeText('#skillsLearnNameHintLabel', t('skillsLearnNameHintLabel'));
      setNodeText('#skillsLearnPathLabel', t('skillsLearnPathLabel'));
      setNodeText('#skillsLearnFileLabel', t('skillsLearnFileLabel'));
      setNodeText('#skillsLearnTextLabel', t('skillsLearnTextLabel'));
      setNodeText('#skillsLearnNameHintMeta', t('skillsLearnNameHintMeta'));
      setNodeText('#skillsLearnPathMeta', t('skillsLearnPathMeta'));
      setNodeText('#skillsLearnFileMeta', t('skillsLearnFileMeta'));
      setNodeText('#skillsLearnTextMeta', t('skillsLearnTextMeta'));
      setText('skillsLearnChooseFiles', 'skillsLearnChooseFiles');
      setText('skillsLearnChooseFolder', 'skillsLearnChooseFolder');
      setText('skillsLearnClearFiles', 'skillsLearnClearFiles');
      const skillsLearnClose = $('skillsLearnClose');
      if (skillsLearnClose) {
        skillsLearnClose.setAttribute('aria-label', t('skillsLearnClose'));
        skillsLearnClose.setAttribute('title', t('skillsLearnClose'));
      }
      setText('skillsLearnCancel', 'cancel');
      setText('skillsLearnStart', 'start');
      setPlaceholder('skillsLearnNameHint', 'skillsLearnNameHintPlaceholder');
      setPlaceholder('skillsLearnPath', 'skillsLearnPathPlaceholder');
      setPlaceholder('skillsLearnText', 'skillsLearnTextPlaceholder');
      updateSkillsLearnFileSummary();
      setNodeText('#skillsListTitle', t('skillsListTitle'));
      setNodeText('#skillsMainTitle', t('skillsMainTitle'));
      setNodeText('#skillsTab .phase-pill span:last-child', t('skillsPhaseLabel'));
      const skillsState = $('skillsState');
      if (skillsState && !skillsState.dataset.dynamic && !state.skills) skillsState.textContent = t('skillsState');
      setLabel('skillName', 'skillsNameLabel', 'name');
      setLabel('skillTags', 'skillsTagsLabel', 'tags');
      setLabel('skillDescription', 'skillsDescLabel', 'description');
      setLabel('skillContent', 'skillsContentLabel', 'markdownContentLabel');
      setPlaceholder('skillName', 'skillsNamePlaceholder');
      setPlaceholder('skillTags', 'skillsTagsPlaceholder');
      setPlaceholder('skillDescription', 'skillsDescPlaceholder');
      setPlaceholder('skillContent', 'skillsContentPlaceholder');
      setText('skillSave', 'skillsSave');
      setText('skillNew', 'skillsNew');
      setText('skillExport', 'skillsExport');
      setText('skillDelete', 'skillsDelete');
      setNodeText('#skillsPreviewTitle', t('skillsPreviewTitle'));
      const empty = $('skillEmptyText');
      if (empty) empty.textContent = t('skillsEmptyPreview');
      if (state.skills) renderSkillsList(state.skills);
    }
    function updateMemoryLang() {
      setNodeText('#memoryHeroTitle', t('memoryTitle'));
      setNodeText('#memoryHeroText', t('memorySubtitle'));
      setNodeText('#memoryAgentLabel', t('memoryAgentLabel'));
      setText('memoryReload', 'memoryReload');
      setText('memoryOrganize', 'memoryOrganize');
      setText('memoryOrganizeFull', 'memoryOrganizeFull');
      setNodeText('#memoryOrganizeHint', t('memoryOrganizeHint'));
      setText('memoryTabCoreBtn', 'memoryTabCore');
      setText('memoryTabLongtermBtn', 'memoryTabLongterm');
      setText('memoryTabVectorBtn', 'memoryTabVector');
      setNodeText('#memoryMainTitle', t('memoryMainTitle'));
      setNodeText('#memoryTab .phase-pill span:last-child', t('memoryPhaseLabel'));
      const memoryState = $('memoryState');
      if (memoryState && !memoryState.dataset.dynamic && !state.memoryItems) memoryState.textContent = t('memoryStateInit');
      setNodeText('#memoryLabelUser', t('memoryLabelUser'));
      setNodeText('#memoryHintUser', t('memoryHintUser'));
      setNodeText('#memoryLabelIdentity', t('memoryLabelIdentity'));
      setNodeText('#memoryHintIdentity', t('memoryHintIdentity'));
      setNodeText('#memoryLabelHot', t('memoryLabelHot'));
      setNodeText('#memoryHintHot', t('memoryHintHot'));
      setNodeText('#memoryLabelCore', t('memoryLabelCore'));
      setNodeText('#memoryHintCore', t('memoryHintCore'));
      setText('memorySaveCore', 'memorySaveCore');
      setNodeText('#memorySnapshotTitle', t('memorySnapshotTitle'));
      setNodeText('#memorySnapshotHint', t('memorySnapshotHint'));
      setText('memorySnapshotRestore', 'memorySnapshotRestore');
      setPlaceholder('memoryUserMd', 'memoryUserPlaceholder');
      setPlaceholder('memoryIdentityMd', 'memoryIdentityPlaceholder');
      setPlaceholder('memoryHotMemory', 'memoryHotPlaceholder');
      setPlaceholder('memoryCoreMemory', 'memoryCorePlaceholder');
      setText('memoryLtmResetBtn', 'memoryLtmReset');
      setText('memoryLtmPrev', 'memoryLtmPrev');
      setText('memoryLtmNext', 'memoryLtmNext');
      setNodeText('#memoryPreviewTitle', t('memoryPreviewTitle'));
      setText('memoryLtmDelete', 'memoryLtmDelete');
      setNodeText('#memoryVectorResultsTitle', t('memoryVectorResultsTitle'));
      setNodeText('#memoryVectorAtlasTitle', t('memoryVectorAtlasTitle'));
      setText('memoryVectorSearchBtn', 'memoryVectorSearch');
      setPlaceholder('memoryVectorQuery', 'memoryVectorSearchPlaceholder');
      const vectorEmpty = $('memoryVectorEmpty');
      if (vectorEmpty) vectorEmpty.textContent = t('memoryVectorEmpty');
      setNodeText('#organizeTitle', t('memoryOrganizeTitle'));
      setNodeText('#organizeDesc', t('memoryOrganizeDesc'));
      const stage = $('organizeStage');
      if (stage && !state.memoryOrganizing) stage.textContent = t('memoryOrganizePrepare');
      const preview = $('memoryLtmPreview');
      if (preview && !state.memorySelectedItem) preview.textContent = t('memoryLtmSelectHint');
      if (state.memoryItems) renderLongTermMemory(state.memoryItems);
      renderVectorMemoryMeta();
      renderVectorSearchResults();
      renderVectorAtlas();
    }

    function addMessage(role, content = '', meta = '', options = {}) {
      const wrap = document.createElement('div');
      wrap.className = `message ${role}`;

      const avatar = document.createElement('div');
      avatar.className = 'avatar';
      applyAvatar(avatar, role);

      const stack = document.createElement('div');
      stack.className = 'stack';
      const m = document.createElement('div');
      m.className = 'meta';
      m.textContent = meta ? meta : (role === 'user' ? t('you') : role === 'assistant' ? agentDisplayName() : t('tool'));
      const card = document.createElement('div');
      card.className = 'card markdown';
      setMarkdownContent(card, content);
      if (options.attachments && options.attachments.length) {
        card.appendChild(renderMessageAttachments(options.attachments));
      }
      stack.append(m, card);
      if (role === 'assistant' && !options.deferAudioControl) {
        attachAudioControl(m, content, options.messageIndex, options.audio);
      }
      wrap.append(avatar, stack);
      const target = options.target || $('chat');
      target.appendChild(wrap);
      if (options.scroll !== false) scrollBottom();
      return { wrap, card, stack, meta: m };
    }

    const CHAT_ATTACHMENT_MAX = 3;
    const CHAT_ATTACHMENT_MAX_BYTES = 128 * 1024 * 1024;
    const CHAT_ATTACHMENT_TOTAL_MAX_BYTES = 512 * 1024 * 1024;
    const CHAT_IMAGE_EXTENSIONS = ['.png','.jpg','.jpeg','.webp','.gif','.bmp'];
    const CHAT_DOCUMENT_EXTENSIONS = ['.txt','.md','.markdown','.log','.ini','.html','.htm','.css','.js','.mjs','.cjs','.ts','.tsx','.jsx','.py','.ps1','.sh','.bat','.cmd','.sql','.pdf','.doc','.docx','.docm','.odt','.rtf','.csv','.tsv','.json','.xml','.yaml','.yml','.toml','.xls','.xlsx','.xlsm','.xlsb','.ods','.ppt','.pptx','.pptm','.odp','.pages','.numbers'];
    const CHAT_ARCHIVE_EXTENSIONS = ['.zip','.rar','.7z','.tar','.gz','.tgz','.bz2','.xz'];

    function chatAttachmentExtension(name) {
      const match = String(name || '').toLowerCase().match(/\.[^.]+$/);
      return match ? match[0] : '';
    }

    function chatAttachmentKind(fileOrName) {
      const name = typeof fileOrName === 'string' ? fileOrName : fileOrName?.name;
      const mime = typeof fileOrName === 'string' ? '' : String(fileOrName?.type || '').toLowerCase();
      const ext = chatAttachmentExtension(name);
      if (CHAT_IMAGE_EXTENSIONS.includes(ext)) return 'image';
      if (CHAT_ARCHIVE_EXTENSIONS.includes(ext)) return 'archive';
      if (CHAT_DOCUMENT_EXTENSIONS.includes(ext)) return 'document';
      if (mime.startsWith('image/')) return 'image';
      if (/(zip|rar|7z|tar|gzip|bzip2|xz|compressed|archive)/i.test(mime)) return 'archive';
      if (/^(text\/|application\/(pdf|json|xml|rtf|msword|vnd\.ms-|vnd\.openxmlformats-|vnd\.oasis\.opendocument|vnd\.apple\.pages|vnd\.apple\.numbers))/i.test(mime)) return 'document';
      return null;
    }

    function chatAttachmentKindLabel(kind) {
      if (kind === 'image') return t('fileImage');
      if (kind === 'archive') return t('fileArchive');
      if (kind === 'document') return t('fileDocument');
      return t('fileAttachment');
    }

    function addChatAttachmentFiles(fileList) {
      if (state.sessionReadOnly) {
        $('hint').textContent = t('sessionReadOnlyHint');
        return;
      }
      const files = Array.from(fileList || []);
      if (!files.length) return;
      const next = state.chatAttachments || [];
      let totalSize = next.reduce(function(total, item) { return total + (item.size || 0); }, 0);
      for (const file of files) {
        if (next.length >= CHAT_ATTACHMENT_MAX) {
          $('hint').textContent = t('fileLimit');
          break;
        }
        const kind = chatAttachmentKind(file);
        if (!kind) {
          $('hint').textContent = t('fileUnsupported') + ': ' + (file.name || '');
          continue;
        }
        if ((file.size || 0) > CHAT_ATTACHMENT_MAX_BYTES) {
          $('hint').textContent = t('fileTooLarge') + ': ' + (file.name || '');
          continue;
        }
        if (totalSize + (file.size || 0) > CHAT_ATTACHMENT_TOTAL_MAX_BYTES) {
          $('hint').textContent = t('fileTotalTooLarge') + ': ' + formatBytes(CHAT_ATTACHMENT_TOTAL_MAX_BYTES);
          continue;
        }
        const key = (file.name || '') + '|' + file.size + '|' + file.lastModified;
        if (next.some(item => item.key === key)) {
          $('hint').textContent = t('fileDuplicate');
          continue;
        }
        next.push({
          key,
          file,
          name: file.name || 'attachment',
          size: file.size || 0,
          mimeType: file.type || '',
          kind,
          previewUrl: kind === 'image' ? URL.createObjectURL(file) : ''
        });
        totalSize += file.size || 0;
      }
      state.chatAttachments = next.slice(0, CHAT_ATTACHMENT_MAX);
      renderAttachmentStrip();
    }

    function clearChatAttachments(revoke = true) {
      (state.chatAttachments || []).forEach(function(item) {
        if (revoke && item.previewUrl) {
          try { URL.revokeObjectURL(item.previewUrl); } catch {}
        }
      });
      state.chatAttachments = [];
      const input = $('chatAttachmentInput');
      if (input) input.value = '';
      renderAttachmentStrip();
    }

    function removeChatAttachment(index) {
      const item = state.chatAttachments?.[index];
      if (item?.previewUrl) {
        try { URL.revokeObjectURL(item.previewUrl); } catch {}
      }
      state.chatAttachments.splice(index, 1);
      renderAttachmentStrip();
    }

    function renderAttachmentStrip() {
      const strip = $('attachmentStrip');
      if (!strip) return;
      strip.innerHTML = '';
      const attachments = state.chatAttachments || [];
      if (!attachments.length) {
        const empty = document.createElement('span');
        empty.className = 'attachment-empty';
        empty.id = 'hint';
        empty.textContent = state.sessionReadOnly ? t('sessionReadOnlyHint') : t('filesEmpty');
        strip.appendChild(empty);
        return;
      }
      attachments.forEach(function(item, index) {
        const chip = document.createElement('div');
        chip.className = 'attachment-chip';
        if (item.kind === 'image' && item.previewUrl) {
          const img = document.createElement('img');
          img.src = item.previewUrl;
          img.alt = item.name;
          chip.appendChild(img);
        } else {
          const icon = document.createElement('span');
          icon.className = 'attachment-icon';
          icon.textContent = item.kind === 'archive' ? 'ARC' : 'DOC';
          chip.appendChild(icon);
        }
        const copy = document.createElement('span');
        copy.className = 'attachment-copy';
        const name = document.createElement('span');
        name.className = 'attachment-name';
        name.textContent = item.name;
        const meta = document.createElement('span');
        meta.className = 'attachment-meta';
        meta.textContent = chatAttachmentKindLabel(item.kind) + ' · ' + formatBytes(item.size);
        copy.append(name, meta);
        const remove = document.createElement('button');
        remove.type = 'button';
        remove.className = 'ghost attachment-remove';
        remove.textContent = '×';
        remove.title = 'Remove';
        remove.addEventListener('click', function() { removeChatAttachment(index); });
        chip.append(copy, remove);
        strip.appendChild(chip);
      });
      const status = document.createElement('span');
      status.className = 'attachment-empty';
      status.id = 'hint';
      status.textContent = attachments.length + '/' + CHAT_ATTACHMENT_MAX;
      strip.appendChild(status);
    }

    function renderMessageAttachments(attachments) {
      const wrap = document.createElement('div');
      wrap.className = 'message-attachments';
      attachments.forEach(function(item) {
        const card = document.createElement('div');
        card.className = 'message-attachment';
        const kind = item.kind || chatAttachmentKind(item.name) || 'document';
        const url = item.url || item.previewUrl || '';
        if (kind === 'image' && url) {
          const img = document.createElement('img');
          img.src = url;
          img.alt = item.name || '';
          card.appendChild(img);
        } else {
          const icon = document.createElement('span');
          icon.className = 'attachment-icon';
          icon.textContent = kind === 'archive' ? 'ARC' : 'DOC';
          card.appendChild(icon);
        }
        const copy = document.createElement('span');
        copy.className = 'attachment-copy';
        const name = document.createElement('span');
        name.className = 'attachment-name';
        name.textContent = item.name || item.fileName || 'attachment';
        const meta = document.createElement('span');
        meta.className = 'attachment-meta';
        meta.textContent = chatAttachmentKindLabel(kind) + (item.size ? ' · ' + formatBytes(item.size) : '');
        copy.append(name, meta);
        card.appendChild(copy);
        wrap.appendChild(card);
      });
      return wrap;
    }

    function effectiveTts() {
      return state.multimodal?.effective?.tts ?? null;
    }

    function effectiveStt() {
      return state.multimodal?.effective?.stt ?? null;
    }

    function hasEffectiveKey(config) {
      return !!(config && (config.hasApiKey || config.has_api_key));
    }

    function canUseTts() {
      const tts = effectiveTts();
      return !!(tts && tts.mode && tts.mode !== 'off' && hasEffectiveKey(tts));
    }

    function canUseStt() {
      const stt = effectiveStt();
      return !!(stt && stt.enabled && speechRecognitionCtor());
    }

    function speechRecognitionCtor() {
      return window.SpeechRecognition || window.webkitSpeechRecognition || null;
    }

    const SOUND_CUE_STORAGE_KEY = 'matdanceSoundCues';
    const SOUND_CUE_DEFAULT_DELAY_MS = 5000;
    const SOUND_CUE_TYPES = [
      { id: 'reply_done', titleKey: 'soundCueTypeReplyDoneTitle', descKey: 'soundCueTypeReplyDoneDesc' },
      { id: 'thinking', titleKey: 'soundCueTypeThinkingTitle', descKey: 'soundCueTypeThinkingDesc' },
      { id: 'confused', titleKey: 'soundCueTypeConfusedTitle', descKey: 'soundCueTypeConfusedDesc' },
      { id: 'help', titleKey: 'soundCueTypeHelpTitle', descKey: 'soundCueTypeHelpDesc' },
      { id: 'confident', titleKey: 'soundCueTypeConfidentTitle', descKey: 'soundCueTypeConfidentDesc' },
      { id: 'low_confidence', titleKey: 'soundCueTypeLowConfidenceTitle', descKey: 'soundCueTypeLowConfidenceDesc' },
      { id: 'idea', titleKey: 'soundCueTypeIdeaTitle', descKey: 'soundCueTypeIdeaDesc' },
      { id: 'happy', titleKey: 'soundCueTypeHappyTitle', descKey: 'soundCueTypeHappyDesc' },
      { id: 'sad', titleKey: 'soundCueTypeSadTitle', descKey: 'soundCueTypeSadDesc' },
      { id: 'perfunctory', titleKey: 'soundCueTypePerfunctoryTitle', descKey: 'soundCueTypePerfunctoryDesc' },
      { id: 'considering', titleKey: 'soundCueTypeConsideringTitle', descKey: 'soundCueTypeConsideringDesc' },
      { id: 'working_hard', titleKey: 'soundCueTypeWorkingHardTitle', descKey: 'soundCueTypeWorkingHardDesc' },
      { id: 'tired', titleKey: 'soundCueTypeTiredTitle', descKey: 'soundCueTypeTiredDesc' },
      { id: 'energized', titleKey: 'soundCueTypeEnergizedTitle', descKey: 'soundCueTypeEnergizedDesc' },
      { id: 'angry', titleKey: 'soundCueTypeAngryTitle', descKey: 'soundCueTypeAngryDesc' },
      { id: 'relieved', titleKey: 'soundCueTypeRelievedTitle', descKey: 'soundCueTypeRelievedDesc' },
      { id: 'awkward', titleKey: 'soundCueTypeAwkwardTitle', descKey: 'soundCueTypeAwkwardDesc' },
      { id: 'surprised', titleKey: 'soundCueTypeSurprisedTitle', descKey: 'soundCueTypeSurprisedDesc' },
      { id: 'apologetic', titleKey: 'soundCueTypeApologeticTitle', descKey: 'soundCueTypeApologeticDesc' },
      { id: 'skeptical', titleKey: 'soundCueTypeSkepticalTitle', descKey: 'soundCueTypeSkepticalDesc' },
      { id: 'alert', titleKey: 'soundCueTypeAlertTitle', descKey: 'soundCueTypeAlertDesc' },
      { id: 'celebrate', titleKey: 'soundCueTypeCelebrateTitle', descKey: 'soundCueTypeCelebrateDesc' },
      { id: 'gentle', titleKey: 'soundCueTypeGentleTitle', descKey: 'soundCueTypeGentleDesc' },
      { id: 'playful', titleKey: 'soundCueTypePlayfulTitle', descKey: 'soundCueTypePlayfulDesc' }
    ];
    const SOUND_CUE_GROUPS = [
      { id: 'flow', titleKey: 'soundCueGroupFlowTitle', descKey: 'soundCueGroupFlowDesc', types: ['reply_done', 'thinking', 'considering', 'working_hard', 'idea', 'alert'] },
      { id: 'positive', titleKey: 'soundCueGroupPositiveTitle', descKey: 'soundCueGroupPositiveDesc', types: ['confident', 'happy', 'energized', 'celebrate', 'relieved', 'playful', 'gentle'] },
      { id: 'uncertain', titleKey: 'soundCueGroupUncertainTitle', descKey: 'soundCueGroupUncertainDesc', types: ['confused', 'help', 'skeptical', 'awkward', 'apologetic', 'surprised'] },
      { id: 'low', titleKey: 'soundCueGroupLowTitle', descKey: 'soundCueGroupLowDesc', types: ['low_confidence', 'sad', 'tired'] },
      { id: 'strong', titleKey: 'soundCueGroupStrongTitle', descKey: 'soundCueGroupStrongDesc', types: ['angry', 'perfunctory'] },
      { id: 'custom', titleKey: 'soundCueGroupCustomTitle', descKey: 'soundCueGroupCustomDesc', types: [], custom: true }
    ];
    const SOUND_CUE_EMOJI = {
      reply_done: '✅',
      thinking: '🌀',
      confused: '❔',
      help: '🫴',
      confident: '✨',
      low_confidence: '◌',
      idea: '💡'
    };
    Object.assign(SOUND_CUE_EMOJI, {
      happy: '\u266a',
      sad: '\u25be',
      perfunctory: '\u2022',
      considering: '?',
      working_hard: '\u25b8',
      tired: '\u25bd',
      energized: '\u26a1',
      angry: '!',
      relieved: '\u2713',
      awkward: '...',
      surprised: '*',
      apologetic: '\u02c5',
      skeptical: '??',
      alert: '\u26a0',
      celebrate: '\u2726',
      gentle: '~',
      playful: '\u266b'
    });
    const SOUND_CUE_ALIASES = {
      reply: 'reply_done', done: 'reply_done', complete: 'reply_done', completed: 'reply_done', reply_done: 'reply_done',
      '完成': 'reply_done', '回复完成': 'reply_done', '结束': 'reply_done',
      thinking: 'thinking', think: 'thinking', pondering: 'thinking',
      '思考': 'thinking', '思考中': 'thinking',
      confused: 'confused', confusion: 'confused', puzzle: 'confused',
      '困惑': 'confused', '疑惑': 'confused', '迷惑': 'confused',
      help: 'help', seeking_help: 'help', need_help: 'help', help_me: 'help',
      '求助': 'help', '帮助': 'help', '需要帮助': 'help', '寻求帮助': 'help',
      confident: 'confident', confidence: 'confident', success: 'confident',
      '自信': 'confident', '确信': 'confident', '有把握': 'confident', '积极': 'confident',
      low_confidence: 'low_confidence', lowconfidence: 'low_confidence', discouraged: 'low_confidence', unsure: 'low_confidence', hit: 'low_confidence',
      '低信心': 'low_confidence', '不确定': 'low_confidence', '受挫': 'low_confidence', '低落': 'low_confidence',
      idea: 'idea', sudden_idea: 'idea', eureka: 'idea', hmm: 'idea',
      '想法': 'idea', '灵感': 'idea', '点子': 'idea', '突然想到': 'idea'
    };
    Object.assign(SOUND_CUE_ALIASES, {
      happy: 'happy', joy: 'happy', joyful: 'happy', glad: 'happy', '\u5f00\u5fc3': 'happy', '\u9ad8\u5174': 'happy',
      sad: 'sad', sadness: 'sad', down: 'sad', '\u96be\u8fc7': 'sad', '\u4f24\u5fc3': 'sad',
      perfunctory: 'perfunctory', casual: 'perfunctory', dismissive: 'perfunctory', '\u6577\u884d': 'perfunctory',
      considering: 'considering', light_thinking: 'considering', slight_thinking: 'considering', '\u7565\u5fae\u601d\u7d22': 'considering', '\u60f3\u4e00\u4e0b': 'considering',
      working_hard: 'working_hard', workinghard: 'working_hard', effort: 'working_hard', trying: 'working_hard', '\u52aa\u529b\u4e2d': 'working_hard',
      tired: 'tired', exhausted: 'tired', fatigue: 'tired', '\u75b2\u60eb': 'tired', '\u7d2f': 'tired',
      energized: 'energized', energetic: 'energized', energy: 'energized', '\u7cbe\u529b\u5145\u6c9b': 'energized',
      angry: 'angry', anger: 'angry', mad: 'angry', annoyed: 'angry', '\u6124\u6012': 'angry', '\u751f\u6c14': 'angry',
      relieved: 'relieved', relief: 'relieved', '\u91ca\u7136': 'relieved', '\u677e\u4e00\u53e3\u6c14': 'relieved',
      awkward: 'awkward', embarrassed: 'awkward', '\u5c34\u5c2c': 'awkward',
      surprised: 'surprised', surprise: 'surprised', shocked: 'surprised', '\u60ca\u8bb6': 'surprised',
      apologetic: 'apologetic', apology: 'apologetic', sorry: 'apologetic', '\u9053\u6b49': 'apologetic', '\u62b1\u6b49': 'apologetic',
      skeptical: 'skeptical', doubt: 'skeptical', doubtful: 'skeptical', suspicious: 'skeptical', '\u6000\u7591': 'skeptical',
      alert: 'alert', warning: 'alert', cautious: 'alert', '\u8b66\u89c9': 'alert', '\u63d0\u9192': 'alert',
      celebrate: 'celebrate', celebration: 'celebrate', victory: 'celebrate', win: 'celebrate', '\u5e86\u795d': 'celebrate',
      gentle: 'gentle', soft: 'gentle', tender: 'gentle', '\u6e29\u67d4': 'gentle',
      playful: 'playful', naughty: 'playful', witty: 'playful', '\u8c03\u76ae': 'playful'
    });
    const DEFAULT_SOUND_CUE_FILES = {
      reply_done: ['reply-normal-1.wav', 'reply-normal-2.wav'],
      thinking: ['thinking-ponder-1.wav', 'thinking-ponder-2.wav'],
      confused: ['confused-1.wav', 'confused-2.wav'],
      help: ['help-urgent-soft-1.wav', 'help-urgent-soft-2.wav'],
      confident: ['confident-1.wav', 'confident-2.wav'],
      low_confidence: ['low-confidence-1.wav', 'low-confidence-2.wav'],
      idea: ['idea-1.wav', 'idea-2.wav'],
      happy: ['happy-1.wav', 'happy-2.wav'],
      sad: ['sad-1.wav', 'sad-2.wav'],
      perfunctory: ['perfunctory-1.wav', 'perfunctory-2.wav'],
      considering: ['considering-1.wav', 'considering-2.wav'],
      working_hard: ['working-hard-1.wav', 'working-hard-2.wav'],
      tired: ['tired-1.wav', 'tired-2.wav'],
      energized: ['energized-1.wav', 'energized-2.wav'],
      angry: ['angry-1.wav', 'angry-2.wav'],
      relieved: ['relieved-1.wav', 'relieved-2.wav'],
      awkward: ['awkward-1.wav', 'awkward-2.wav'],
      surprised: ['surprised-1.wav', 'surprised-2.wav'],
      apologetic: ['apologetic-1.wav', 'apologetic-2.wav'],
      skeptical: ['skeptical-1.wav', 'skeptical-2.wav'],
      alert: ['alert-1.wav', 'alert-2.wav'],
      celebrate: ['celebrate-1.wav', 'celebrate-2.wav'],
      gentle: ['gentle-1.wav', 'gentle-2.wav'],
      playful: ['playful-1.wav', 'playful-2.wav']
    };

    function soundCueToken(value) {
      const raw = String(value || '').trim().toLowerCase().replace(/[:\uFF1A]+$/g, '');
      const compact = raw.replace(/[\s-]+/g, '_');
      const ascii = compact.replace(/[^a-z0-9_]/g, '').replace(/^_+|_+$/g, '');
      return { raw, compact, ascii };
    }

    function customSoundCueDefinitions(settings = state.soundCues) {
      return (Array.isArray(settings?.customTypes) ? settings.customTypes : [])
        .filter(item => item && item.id && item.name)
        .map(function(item) {
          return {
            id: String(item.id),
            name: String(item.name || item.id),
            desc: String(item.desc || ''),
            aliases: Array.isArray(item.aliases) ? item.aliases.filter(Boolean).map(String) : [],
            custom: true
          };
        });
    }

    function findCustomSoundCueDefinition(value, settings = state.soundCues) {
      const token = soundCueToken(value);
      if (!token.raw && !token.ascii) return null;
      return customSoundCueDefinitions(settings).find(function(item) {
        const candidates = [item.id, item.name].concat(item.aliases || []);
        return candidates.some(function(candidate) {
          const next = soundCueToken(candidate);
          return next.compact === token.compact || (!!next.ascii && next.ascii === token.ascii);
        });
      }) || null;
    }

    function normalizeSoundCueType(value, settings = state.soundCues) {
      const token = soundCueToken(value);
      if (!token.raw && !token.ascii) return null;
      const builtIn = SOUND_CUE_ALIASES[token.compact]
        || SOUND_CUE_ALIASES[token.ascii]
        || (SOUND_CUE_TYPES.some(type => type.id === token.ascii) ? token.ascii : null);
      if (builtIn) return builtIn;
      let custom = findCustomSoundCueDefinition(value, settings);
      if (!custom && !settings && !state.soundCues) {
        try { custom = findCustomSoundCueDefinition(value, loadSoundCueSettings()); }
        catch { custom = null; }
      }
      return custom?.id || null;
    }

    function safeCustomSoundCueId(value) {
      const token = soundCueToken(value);
      const base = token.ascii || ('custom_' + Date.now().toString(36));
      const compact = base.startsWith('custom_') ? base : 'custom_' + base;
      return compact.slice(0, 56);
    }

    function uniqueCustomSoundCueId(name, settings, preferred) {
      const used = new Set(SOUND_CUE_TYPES.map(type => type.id));
      Object.keys(settings?.types || {}).forEach(id => used.add(id));
      customSoundCueDefinitions(settings).forEach(type => used.add(type.id));
      let base = safeCustomSoundCueId(preferred || name);
      if (!base || base === 'custom_') base = 'custom_' + Date.now().toString(36);
      let id = base;
      let index = 2;
      while (used.has(id)) {
        id = (base.slice(0, 50) + '_' + index).slice(0, 56);
        index++;
      }
      return id;
    }

    function defaultSoundCueUrl(fileName) {
      return '/api/sound-cue/default/' + encodeURIComponent(fileName);
    }

    function defaultSoundCueItems(type) {
      return (DEFAULT_SOUND_CUE_FILES[type] || []).map(function(fileName, index) {
        return {
          id: 'default:' + type + ':' + index,
          name: fileName.replace(/\.wav$/i, ''),
          url: defaultSoundCueUrl(fileName),
          source: 'default',
          enabled: true
        };
      });
    }

    function normalizeSoundCueTypeConfig(saved, typeId) {
      const disabledItemIds = new Set(Array.isArray(saved?.disabledItemIds) ? saved.disabledItemIds.filter(Boolean).map(String) : []);
      const custom = Array.isArray(saved?.custom)
        ? saved.custom.filter(item => item && (item.url || item.dataUrl)).map(function(item, index) {
            const id = String(item.id || ('custom:' + typeId + ':' + index + ':' + Date.now()));
            if (item.enabled === false) disabledItemIds.add(id);
            return {
              id,
              name: String(item.name || item.fileName || typeId),
              url: item.dataUrl || item.url,
              dataUrl: item.dataUrl || null,
              relativePath: item.relativePath || null,
              source: 'custom',
              enabled: !disabledItemIds.has(id)
            };
          })
        : [];
      return {
        enabled: saved?.enabled !== false,
        disabledItemIds: Array.from(disabledItemIds),
        custom
      };
    }

    function defaultSoundCueSettings() {
      const types = {};
      SOUND_CUE_TYPES.forEach(function(type) {
        types[type.id] = { enabled: true, disabledItemIds: [], custom: [] };
      });
      return { enabled: true, volume: 0.65, delayMs: SOUND_CUE_DEFAULT_DELAY_MS, types, customTypes: [] };
    }

    function loadSoundCueSettings() {
      if (state.soundCues) return state.soundCues;
      let stored = null;
      try { stored = JSON.parse(localStorage.getItem(SOUND_CUE_STORAGE_KEY) || 'null'); }
      catch { stored = null; }
      const defaults = defaultSoundCueSettings();
      const next = {
        enabled: stored?.enabled !== false,
        volume: Number.isFinite(Number(stored?.volume)) ? Math.max(0, Math.min(1, Number(stored.volume))) : defaults.volume,
        delayMs: Number.isFinite(Number(stored?.delayMs)) ? Math.max(0, Math.min(30000, Number(stored.delayMs))) : defaults.delayMs,
        types: {},
        customTypes: []
      };
      (Array.isArray(stored?.customTypes) ? stored.customTypes : []).forEach(function(item) {
        const name = String(item?.name || '').trim();
        if (!name) return;
        const id = uniqueCustomSoundCueId(name, next, item?.id);
        const aliases = Array.from(new Set([name].concat(Array.isArray(item?.aliases) ? item.aliases : []).filter(Boolean).map(String)));
        next.customTypes.push({
          id,
          name,
          desc: String(item?.desc || ''),
          aliases,
          custom: true,
          importId: item?.id || id
        });
      });
      SOUND_CUE_TYPES.forEach(function(type) {
        const saved = stored?.types?.[type.id] || {};
        next.types[type.id] = normalizeSoundCueTypeConfig(saved, type.id);
        ensureSoundCueTypeHasEnabledItem(next, type.id);
      });
      next.customTypes.forEach(function(type) {
        const saved = stored?.types?.[type.id] || stored?.types?.[type.importId] || {};
        next.types[type.id] = normalizeSoundCueTypeConfig(saved, type.id);
        delete type.importId;
        ensureSoundCueTypeHasEnabledItem(next, type.id);
      });
      state.soundCues = next;
      return next;
    }

    function saveSoundCueSettings() {
      const settings = loadSoundCueSettings();
      localStorage.setItem(SOUND_CUE_STORAGE_KEY, JSON.stringify(settings));
      syncSoundCueSettingsToServer(settings);
      return settings;
    }

    function soundCueSettingsServerPayload(settings) {
      const copy = JSON.parse(JSON.stringify(settings || loadSoundCueSettings()));
      Object.values(copy.types || {}).forEach(function(config) {
        (config.custom || []).forEach(function(item) {
          delete item.dataUrl;
          delete item.blob;
        });
      });
      return copy;
    }

    function syncSoundCueSettingsToServer(settings, options = {}) {
      if (!state.agent) return;
      const send = function() {
        const payload = soundCueSettingsServerPayload(settings);
        api('/api/sound-cue-settings', {
          method: 'POST',
          headers: { 'content-type': 'application/json' },
          body: JSON.stringify({ agent: state.agent, settings: payload })
        }).catch(function(err) {
          console.warn('Failed to sync sound cue settings', err);
        });
      };
      if (options.immediate) {
        send();
        return;
      }
      if (state.soundCueSaveTimer) clearTimeout(state.soundCueSaveTimer);
      state.soundCueSaveTimer = setTimeout(function() {
        state.soundCueSaveTimer = null;
        send();
      }, 250);
    }

    function ensureSoundCueTypeHasEnabledItem(settings, type) {
      const normalized = normalizeSoundCueType(type, settings);
      const typeConfig = settings?.types?.[normalized];
      if (!normalized || !typeConfig || typeConfig.enabled === false) return;
      const disabled = new Set(Array.isArray(typeConfig.disabledItemIds) ? typeConfig.disabledItemIds : []);
      const custom = Array.isArray(typeConfig.custom) ? typeConfig.custom : [];
      const items = defaultSoundCueItems(normalized).concat(custom);
      if (!items.length) return;
      if (items.some(item => item?.url && !disabled.has(item.id))) return;
      disabled.delete(items[0].id);
      typeConfig.disabledItemIds = Array.from(disabled);
    }

    function soundCueDisplayDelayMs() {
      const settings = loadSoundCueSettings();
      return Math.max(0, Math.min(30000, Number(settings.delayMs ?? SOUND_CUE_DEFAULT_DELAY_MS) || 0));
    }

    function soundCueItems(type, options = {}) {
      const settings = loadSoundCueSettings();
      const normalized = normalizeSoundCueType(type, settings);
      if (!normalized) return [];
      const config = settings.types[normalized] || { custom: [], disabledItemIds: [] };
      const disabled = new Set(Array.isArray(config.disabledItemIds) ? config.disabledItemIds : []);
      return defaultSoundCueItems(normalized).concat(config.custom || []).map(function(item) {
        return { ...item, enabled: options.ignoreDisabled ? true : !disabled.has(item.id) };
      });
    }

    function playAudioMarkerRegex() {
      return /`{1,3}\s*\{play[_-]?audio\s*[:\uFF1A]\s*([^}]+)\}\s*`{1,3}|\{play[_-]?audio\s*[:\uFF1A]\s*([^}]+)\}/gi;
    }

    function wrappedPlayAudioMarkerRegex() {
      return /`{1,3}\s*\{play[_-]?audio\s*[:\uFF1A]\s*[^}]+\}\s*`{1,3}/gi;
    }

    function stripPlayAudioMarkers(text) {
      return String(text || '')
        .replace(wrappedPlayAudioMarkerRegex(), '')
        .replace(playAudioMarkerRegex(), '')
        .replace(/`{0,3}\s*\{play(?:[_-]?audio)?\s*(?:[:\uFF1A]\s*[^}]*)?$/i, '');
    }

    function protectSoundCueMarkers(text, soundCues) {
      return String(text || '')
        .replace(playAudioMarkerRegex(), function(_, wrappedValue, plainValue) {
          const value = wrappedValue || plainValue;
          const normalized = normalizeSoundCueType(value);
          if (!normalized) return '';
          const token = `§SOUNDCUE${soundCues.length}§`;
          soundCues.push(normalized);
          return `\n${token}\n`;
        })
        .replace(/`{0,3}\s*\{play(?:[_-]?audio)?\s*(?:[:\uFF1A]\s*[^}]*)?$/i, '');
    }

    function extractPlayAudioMarkers(text) {
      const found = [];
      String(text || '').replace(playAudioMarkerRegex(), function(_, wrappedValue, plainValue) {
        const value = wrappedValue || plainValue;
        const normalized = normalizeSoundCueType(value);
        if (normalized) found.push(normalized);
        return '';
      });
      return found;
    }

    function createPlayAudioMarkerStream() {
      return { pending: '' };
    }

    function pendingPlayAudioMarkerStart(text) {
      const value = String(text || '');
      const lower = value.toLowerCase();
      const explicit = lower.lastIndexOf('{play');
      if (explicit >= 0) {
        const prefix = value.slice(Math.max(0, explicit - 6), explicit);
        const wrapped = prefix.match(/`{1,3}\s*$/);
        return wrapped ? explicit - wrapped[0].length : explicit;
      }
      const min = Math.max(0, value.length - 16);
      const markerStart = '{play';
      for (let index = value.length - 1; index >= min; index--) {
        const candidate = lower.slice(index).replace(/^`{1,3}\s*/, '');
        if (candidate && markerStart.startsWith(candidate)) return index;
      }
      return -1;
    }

    function consumePlayAudioMarkerStream(stream, text) {
      if (!stream || !text) return [];
      const source = String(stream.pending || '') + String(text || '');
      const pieces = [];
      const regex = playAudioMarkerRegex();
      let lastEnd = 0;
      let match;
      while ((match = regex.exec(source)) !== null) {
        if (match.index > lastEnd) {
          pieces.push({ type: 'text', text: source.slice(lastEnd, match.index) });
        }
        const normalized = normalizeSoundCueType(match[1] || match[2]);
        if (normalized) {
          pieces.push({ type: 'cue', cue: normalized });
        }
        lastEnd = match.index + match[0].length;
      }

      const tail = lastEnd > 0 ? source.slice(lastEnd) : source;
      const markerStart = pendingPlayAudioMarkerStart(tail);
      const visibleTail = markerStart >= 0 ? tail.slice(0, markerStart) : tail;
      if (visibleTail) {
        pieces.push({ type: 'text', text: visibleTail });
      }
      stream.pending = markerStart >= 0 ? tail.slice(markerStart) : tail.slice(-64);
      if (markerStart < 0) stream.pending = '';
      return pieces;
    }

    function splitPlayAudioMarkerContent(text) {
      const stream = createPlayAudioMarkerStream();
      const pieces = consumePlayAudioMarkerStream(stream, text);
      const leftover = stripPlayAudioMarkers(stream.pending || '');
      if (leftover) pieces.push({ type: 'text', text: leftover });
      stream.pending = '';
      return pieces;
    }

    function pickSoundCueItem(type, options = {}) {
      const settings = loadSoundCueSettings();
      const normalized = normalizeSoundCueType(type, settings);
      if (!normalized) return null;
      const typeConfig = settings.types[normalized];
      if (!options.force && (!settings.enabled || typeConfig?.enabled === false)) return null;
      const includeDisabled = !!options.itemId;
      const items = soundCueItems(normalized, { ignoreDisabled: includeDisabled }).filter(item => item?.url && (includeDisabled || item.enabled !== false));
      if (options.itemId) return items.find(item => item.id === options.itemId) || null;
      if (!items.length) return null;
      return items[Math.floor(Math.random() * items.length)];
    }

    function primeSoundCueAudio() {
      if (state.soundCuePrimed) return;
      state.soundCuePrimed = true;
      try {
        const silent = new Audio('data:audio/wav;base64,UklGRiQAAABXQVZFZm10IBAAAAABAAEAESsAACJWAAACABAAZGF0YQAAAAA=');
        silent.volume = 0;
        silent.play().catch(function() { state.soundCuePrimed = false; });
      } catch {
        state.soundCuePrimed = false;
      }
    }

    function resolveSoundCueIdleIfNeeded() {
      if (state.soundCuePlaying || state.soundCueQueue.length) return;
      const resolvers = state.soundCueIdleResolvers.splice(0);
      resolvers.forEach(function(resolve) { resolve(); });
    }

    function waitForSoundCueQueueIdle(timeoutMs = 12000) {
      if (!state.soundCuePlaying && !state.soundCueQueue.length) return Promise.resolve();
      return new Promise(function(resolve) {
        let done = false;
        const finish = function() {
          if (done) return;
          done = true;
          resolve();
        };
        state.soundCueIdleResolvers.push(finish);
        setTimeout(finish, Math.max(0, Number(timeoutMs) || 0));
      });
    }

    function processSoundCueQueue() {
      if (state.soundCuePlaying) return;
      const next = state.soundCueQueue.shift();
      if (!next?.url) {
        resolveSoundCueIdleIfNeeded();
        return;
      }
      state.soundCuePlaying = true;
      const player = new Audio(next.url);
      player.volume = Math.max(0, Math.min(1, loadSoundCueSettings().volume));
      state.soundCuePlayer = player;
      const notifyStarted = function(started) {
        if (typeof next.onStarted === 'function') {
          const callback = next.onStarted;
          next.onStarted = null;
          callback(!!started);
        }
      };
      const notifyFinished = function(finished) {
        if (typeof next.onFinished === 'function') {
          const callback = next.onFinished;
          next.onFinished = null;
          callback(!!finished);
        }
      };
      const clearAndContinue = function(delay = 70, finished = false) {
        notifyStarted(false);
        notifyFinished(finished);
        if (state.soundCuePlayer === player) state.soundCuePlayer = null;
        state.soundCuePlaying = false;
        setTimeout(function() {
          processSoundCueQueue();
          resolveSoundCueIdleIfNeeded();
        }, delay);
      };
      player.onended = function() { clearAndContinue(70, true); };
      player.onerror = function() { clearAndContinue(0); };
      player.onabort = function() { clearAndContinue(0); };
      player.play().then(function() {
        notifyStarted(true);
      }).catch(function(err) {
        clearAndContinue(0);
        const blocked = err && (err.name === 'NotAllowedError' || String(err.message || err).toLowerCase().includes('user'));
        if (blocked && !state.soundCueBlocked) {
          state.soundCueBlocked = true;
          const hint = $('hint');
          if (hint) hint.textContent = state.lang === 'zh' ? '浏览器拦截了提示音；请点击 Settings > Sound 的预览按钮解锁音频。' : 'The browser blocked sound cues; click a preview button in Settings > Sound to unlock audio.';
        }
      });
    }

    function enqueueSoundCue(item) {
      if (!item?.url) {
        if (typeof item?.onStarted === 'function') item.onStarted(false);
        return;
      }
      state.soundCueQueue.push(item);
      if (state.soundCueQueue.length > 48) {
        const dropped = state.soundCueQueue.splice(0, state.soundCueQueue.length - 48);
        dropped.forEach(function(entry) {
          if (typeof entry?.onStarted === 'function') entry.onStarted(false);
          if (typeof entry?.onFinished === 'function') entry.onFinished(false);
        });
      }
      processSoundCueQueue();
    }

    function playSoundCue(type, options = {}) {
      let resolveStarted = null;
      let resolveFinished = null;
      const started = options.waitUntilStarted ? new Promise(resolve => { resolveStarted = resolve; }) : null;
      const finished = options.waitUntilEnded ? new Promise(resolve => { resolveFinished = resolve; }) : null;
      const finishWithoutPlayback = function() {
        if (resolveStarted) resolveStarted(false);
        if (resolveFinished) resolveFinished(false);
        return finished || started || false;
      };
      const settings = loadSoundCueSettings();
      const normalized = normalizeSoundCueType(type, settings);
      if (!normalized) return finishWithoutPlayback();
      const now = Date.now();
      if (!options.force && state.soundCueLastPlayedAt[normalized] && now - state.soundCueLastPlayedAt[normalized] < 420) return finishWithoutPlayback();
      const item = pickSoundCueItem(normalized, options);
      if (!item?.url) return finishWithoutPlayback();
      state.soundCueLastPlayedAt[normalized] = now;
      primeSoundCueAudio();
      enqueueSoundCue({ ...item, onStarted: resolveStarted, onFinished: resolveFinished });
      return finished || started || true;
    }

    function playSoundCueMarkers(text, options = {}) {
      const played = new Set();
      extractPlayAudioMarkers(text).forEach(function(type) {
        if (options.skip?.has?.(type)) return;
        if (played.has(type)) return;
        played.add(type);
        playSoundCue(type);
      });
      return played;
    }

    function wait(ms) {
      return new Promise(resolve => setTimeout(resolve, Math.max(0, Number(ms) || 0)));
    }

    function createOrderedUiQueue() {
      let chain = Promise.resolve();
      return {
        enqueue(action) {
          chain = chain
            .then(() => action())
            .catch(err => {
              console.warn('Queued UI event failed', err);
            });
          return chain;
        },
        drain() {
          return chain;
        }
      };
    }

    function soundCueDefinition(type) {
      const settings = loadSoundCueSettings();
      const normalized = normalizeSoundCueType(type, settings);
      return SOUND_CUE_TYPES.find(item => item.id === normalized) || findCustomSoundCueDefinition(normalized, settings) || null;
    }

    function soundCueDisplayName(type) {
      const settings = loadSoundCueSettings();
      const normalized = normalizeSoundCueType(type, settings);
      const definition = soundCueDefinition(normalized);
      if (!definition) return normalized || String(type || '');
      return definition.custom ? definition.name : t(definition.titleKey);
    }

    function soundCueDescription(type) {
      const settings = loadSoundCueSettings();
      const normalized = normalizeSoundCueType(type, settings);
      const definition = soundCueDefinition(normalized);
      if (!definition) return '';
      return definition.custom ? (definition.desc || '') : t(definition.descKey);
    }

    function soundCueEmoji(type) {
      const settings = loadSoundCueSettings();
      const normalized = normalizeSoundCueType(type, settings);
      return SOUND_CUE_EMOJI[normalized] || '\u25c7';
    }

    function createSoundCueEvent(cue, context, options = {}) {
      const block = document.createElement('div');
      block.className = 'sound-cue-event ' + (context === 'thinking' ? 'thinking' : 'reply');
      if (options.played) block.classList.add('played');
      if (options.static) block.classList.add('static');

      const icon = document.createElement('div');
      icon.className = 'sound-cue-event-icon';
      icon.textContent = soundCueEmoji(cue);

      const main = document.createElement('div');
      main.className = 'sound-cue-event-main';
      const title = document.createElement('div');
      title.className = 'sound-cue-event-title';
      title.textContent = soundCueDisplayName(cue);
      const badge = document.createElement('div');
      badge.className = 'sound-cue-event-context';
      badge.textContent = context === 'thinking' ? t('soundCueEventThinking') : t('soundCueEventReply');
      main.append(title, badge);

      const status = document.createElement('div');
      status.className = 'sound-cue-event-status';
      status.title = t('soundCueEventDelay');
      const spinner = document.createElement('span');
      spinner.className = 'sound-cue-spinner';
      const statusText = document.createElement('span');
      statusText.dataset.soundCueStatusText = 'true';
      statusText.textContent = options.played || options.static ? t('soundCueEventSaved') : t('soundCueEventWaiting');
      status.append(spinner, statusText);

      const body = document.createElement('div');
      body.className = 'sound-cue-event-body markdown';
      body.hidden = true;
      body.dataset.soundCueBody = 'true';

      block.append(icon, main, status, body);
      return block;
    }

    function soundCueEventBody(block) {
      return block?.querySelector?.('[data-sound-cue-body]') || null;
    }

    function markSoundCueEventPlayed(block) {
      if (!block) return;
      block.classList.add('played');
      const statusText = block.querySelector('[data-sound-cue-status-text]');
      if (statusText) statusText.textContent = t('soundCueEventSaved');
    }

    function renderSoundCueSettings() {
      const settings = loadSoundCueSettings();
      const enabled = $('soundCueEnabled');
      const volume = $('soundCueVolume');
      const volumeValue = $('soundCueVolumeValue');
      const delay = $('soundCueDelay');
      const delayValue = $('soundCueDelayValue');
      if (enabled) enabled.checked = settings.enabled;
      if (volume) volume.value = Math.round(settings.volume * 100);
      if (volumeValue) volumeValue.textContent = Math.round(settings.volume * 100) + '%';
      if (delay) delay.value = soundCueDisplayDelayMs();
      if (delayValue) delayValue.textContent = (soundCueDisplayDelayMs() / 1000).toFixed(1) + 's';

      const list = $('soundCueList');
      if (!list) return;
      list.innerHTML = '';
      const byId = new Map(SOUND_CUE_TYPES.map(type => [type.id, type]));
      customSoundCueDefinitions(settings).forEach(type => byId.set(type.id, type));
      const groupTypeIds = function(group) {
        return group.custom ? customSoundCueDefinitions(settings).map(type => type.id) : group.types;
      };
      const renderTypeRow = function(type, parent) {
        settings.types[type.id] = settings.types[type.id] || { enabled: true, disabledItemIds: [], custom: [] };
        const typeConfig = settings.types[type.id];
        const items = soundCueItems(type.id);
        const enabledItems = items.filter(item => item.enabled !== false);
        const row = document.createElement('section');
        row.className = 'sound-cue-row' + (typeConfig.enabled === false ? ' off' : '') + (type.custom ? ' custom-type' : '');

        const head = document.createElement('div');
        head.className = 'sound-cue-head';
        const toggle = document.createElement('label');
        toggle.className = 'sound-cue-toggle';
        const toggleText = document.createElement('span');
        toggleText.textContent = soundCueDisplayName(type.id);
        const toggleInput = document.createElement('input');
        toggleInput.type = 'checkbox';
        toggleInput.checked = typeConfig.enabled !== false;
        toggleInput.dataset.soundToggle = type.id;
        toggle.append(toggleText, toggleInput);
        const desc = document.createElement('div');
        desc.className = 'sound-cue-desc';
        desc.textContent = soundCueDescription(type.id) || type.id;
        head.append(toggle, desc);

        const customEdit = document.createElement('div');
        if (type.custom) {
          customEdit.className = 'sound-cue-custom-edit';
          const nameInput = document.createElement('input');
          nameInput.type = 'text';
          nameInput.value = type.name || '';
          nameInput.placeholder = t('soundCueCustomNamePlaceholder');
          nameInput.dataset.soundCustomName = type.id;
          const descInput = document.createElement('input');
          descInput.type = 'text';
          descInput.value = type.desc || '';
          descInput.placeholder = t('soundCueCustomDescPlaceholder');
          descInput.dataset.soundCustomDesc = type.id;
          customEdit.append(nameInput, descInput);
        }

        const body = document.createElement('div');
        body.className = 'sound-cue-body';
        const actions = document.createElement('div');
        actions.className = 'sound-cue-actions';
        const preview = document.createElement('button');
        preview.className = 'ghost';
        preview.type = 'button';
        preview.dataset.soundPreview = type.id;
        preview.textContent = t('soundCuePreview');
        const upload = document.createElement('button');
        upload.className = 'primary';
        upload.type = 'button';
        upload.dataset.soundUpload = type.id;
        upload.textContent = t('soundCueUpload');
        actions.append(preview, upload);

        const assets = document.createElement('div');
        assets.className = 'sound-cue-assets';
        if (!items.length) {
          const empty = document.createElement('div');
          empty.className = 'sound-cue-empty';
          empty.textContent = t('empty');
          assets.appendChild(empty);
        } else {
          items.forEach(function(item) {
            const chip = document.createElement('div');
            chip.className = 'sound-cue-asset ' + (item.source === 'custom' ? 'custom' : 'default') + (item.enabled === false ? ' disabled' : '');
            const source = document.createElement('span');
            source.className = 'sound-cue-source';
            source.textContent = item.source === 'custom' ? t('soundCueCustom') : t('soundCueDefault');
            const name = document.createElement('span');
            name.className = 'sound-cue-asset-name';
            name.textContent = item.name || type.id;
            const itemActions = document.createElement('span');
            itemActions.className = 'sound-cue-asset-actions';
            const isOnlyEnabledItem = typeConfig.enabled !== false && item.enabled !== false && enabledItems.length <= 1;
            const assetToggleLabel = document.createElement('label');
            assetToggleLabel.className = 'sound-cue-asset-toggle' + (isOnlyEnabledItem ? ' disabled-toggle' : '');
            assetToggleLabel.title = isOnlyEnabledItem ? t('soundCueNeedOneEnabled') : t('soundCueAssetToggle');
            const assetToggle = document.createElement('input');
            assetToggle.type = 'checkbox';
            assetToggle.title = t('soundCueAssetToggle');
            assetToggle.checked = item.enabled !== false;
            assetToggle.dataset.soundItemToggle = item.id;
            assetToggle.dataset.soundType = type.id;
            assetToggle.disabled = isOnlyEnabledItem;
            const assetToggleText = document.createElement('span');
            assetToggleText.textContent = item.enabled === false ? t('soundCueAssetDisabled') : t('soundCueAssetEnabled');
            assetToggleLabel.append(assetToggle, assetToggleText);
            itemActions.appendChild(assetToggleLabel);
            const previewItem = document.createElement('button');
            previewItem.type = 'button';
            previewItem.title = t('soundCuePreview');
            previewItem.dataset.soundPreviewItem = item.id;
            previewItem.dataset.soundPreviewType = type.id;
            previewItem.textContent = '▶';
            itemActions.appendChild(previewItem);
            if (item.source === 'custom') {
              const remove = document.createElement('button');
              remove.type = 'button';
              remove.title = t('soundCueRemove');
              remove.dataset.soundRemove = item.id;
              remove.dataset.soundType = type.id;
              remove.textContent = '×';
              itemActions.appendChild(remove);
            }
            chip.append(source, name, itemActions);
            assets.appendChild(chip);
          });
        }
        body.append(actions, assets);
        row.appendChild(head);
        if (type.custom) {
          const customButtons = document.createElement('div');
          customButtons.className = 'sound-cue-custom-buttons';
          const save = document.createElement('button');
          save.className = 'ghost';
          save.type = 'button';
          save.dataset.soundCustomSave = type.id;
          save.textContent = t('soundCueCustomSave');
          const removeType = document.createElement('button');
          removeType.className = 'danger';
          removeType.type = 'button';
          removeType.dataset.soundCustomDelete = type.id;
          removeType.textContent = t('soundCueCustomDelete');
          customButtons.append(save, removeType);
          row.append(customEdit, customButtons);
        }
        row.appendChild(body);
        parent.appendChild(row);
      };
      const activeGroup = SOUND_CUE_GROUPS.find(group => group.id === state.soundCueGroup) || SOUND_CUE_GROUPS[0];
      state.soundCueGroup = activeGroup.id;
      const board = document.createElement('div');
      board.className = 'sound-cue-board';

      const nav = document.createElement('div');
      nav.className = 'sound-cue-group-nav';
      SOUND_CUE_GROUPS.forEach(function(group) {
        const tab = document.createElement('button');
        tab.type = 'button';
        tab.className = 'sound-cue-group-tab' + (group.id === activeGroup.id ? ' active' : '');
        tab.dataset.soundGroup = group.id;
        const tabTitle = document.createElement('span');
        tabTitle.className = 'sound-cue-group-tab-title';
        tabTitle.textContent = t(group.titleKey);
        const tabCount = document.createElement('span');
        tabCount.className = 'sound-cue-group-tab-count';
        tabCount.textContent = String(groupTypeIds(group).length);
        tab.append(tabTitle, tabCount);
        nav.appendChild(tab);
      });

      const panel = document.createElement('section');
      panel.className = 'sound-cue-group-panel';
      const head = document.createElement('div');
      head.className = 'sound-cue-group-head';
      const copy = document.createElement('div');
      copy.className = 'settings-card-copy';
      const title = document.createElement('div');
      title.className = 'sound-cue-group-title';
      title.textContent = t(activeGroup.titleKey);
      const desc = document.createElement('div');
      desc.className = 'sound-cue-group-desc';
      desc.textContent = t(activeGroup.descKey);
      copy.append(title, desc);
      const count = document.createElement('div');
      count.className = 'sound-cue-group-count';
      const activeTypeIds = groupTypeIds(activeGroup);
      count.textContent = activeTypeIds.length + ' ' + t('soundCueTypeCount');
      head.append(copy, count);
      const body = document.createElement('div');
      body.className = 'sound-cue-group-body';
      if (activeGroup.custom) {
        const form = document.createElement('div');
        form.className = 'sound-cue-custom-form';
        const name = document.createElement('input');
        name.type = 'text';
        name.placeholder = t('soundCueCustomNamePlaceholder');
        name.dataset.soundCustomNewName = 'true';
        const descInput = document.createElement('input');
        descInput.type = 'text';
        descInput.placeholder = t('soundCueCustomDescPlaceholder');
        descInput.dataset.soundCustomNewDesc = 'true';
        const add = document.createElement('button');
        add.className = 'primary';
        add.type = 'button';
        add.dataset.soundCustomAdd = 'true';
        add.textContent = t('soundCueCustomAdd');
        form.append(name, descInput, add);
        panel.append(head, form);
      } else {
        panel.appendChild(head);
      }
      activeTypeIds.forEach(function(typeId) {
        const type = byId.get(typeId);
        if (type) renderTypeRow(type, body);
      });
      if (!activeTypeIds.length) {
        const empty = document.createElement('div');
        empty.className = 'sound-cue-custom-empty';
        empty.textContent = t('soundCueCustomEmpty');
        body.appendChild(empty);
      }
      panel.appendChild(body);
      board.append(nav, panel);
      list.appendChild(board);
    }

    function setSoundCueTypeEnabled(type, enabled) {
      const settings = loadSoundCueSettings();
      const normalized = normalizeSoundCueType(type, settings);
      if (!normalized) return;
      settings.types[normalized] = settings.types[normalized] || { enabled: true, disabledItemIds: [], custom: [] };
      settings.types[normalized].enabled = !!enabled;
      ensureSoundCueTypeHasEnabledItem(settings, normalized);
      saveSoundCueSettings();
      renderSoundCueSettings();
    }

    function setSoundCueAssetEnabled(type, itemId, enabled) {
      const settings = loadSoundCueSettings();
      const normalized = normalizeSoundCueType(type, settings);
      if (!normalized || !itemId) return;
      const typeConfig = settings.types[normalized] = settings.types[normalized] || { enabled: true, disabledItemIds: [], custom: [] };
      const disabled = new Set(Array.isArray(typeConfig.disabledItemIds) ? typeConfig.disabledItemIds : []);
      if (enabled) {
        disabled.delete(itemId);
      } else {
        const enabledItems = soundCueItems(normalized).filter(item => item.id !== itemId && item.enabled !== false && item.url);
        if (typeConfig.enabled !== false && enabledItems.length === 0) {
          const hint = $('hint');
          if (hint) hint.textContent = t('soundCueNeedOneEnabled');
          renderSoundCueSettings();
          return;
        }
        disabled.add(itemId);
      }
      typeConfig.disabledItemIds = Array.from(disabled);
      saveSoundCueSettings();
      renderSoundCueSettings();
    }

    function removeSoundCueAsset(type, itemId) {
      const settings = loadSoundCueSettings();
      const normalized = normalizeSoundCueType(type, settings);
      if (!normalized || !itemId) return;
      const typeConfig = settings.types[normalized];
      if (!typeConfig) return;
      typeConfig.custom = (typeConfig.custom || []).filter(item => item.id !== itemId);
      typeConfig.disabledItemIds = (typeConfig.disabledItemIds || []).filter(id => id !== itemId);
      ensureSoundCueTypeHasEnabledItem(settings, normalized);
      saveSoundCueSettings();
      renderSoundCueSettings();
    }

    function addCustomSoundCueType() {
      const settings = loadSoundCueSettings();
      const nameInput = document.querySelector('[data-sound-custom-new-name]');
      const descInput = document.querySelector('[data-sound-custom-new-desc]');
      const name = String(nameInput?.value || '').trim();
      const desc = String(descInput?.value || '').trim();
      if (!name) {
        const hint = $('hint');
        if (hint) hint.textContent = t('soundCueCustomNamePlaceholder');
        return;
      }
      const id = uniqueCustomSoundCueId(name, settings);
      settings.customTypes = settings.customTypes || [];
      settings.customTypes.push({ id, name, desc, aliases: [name], custom: true });
      settings.types[id] = { enabled: true, disabledItemIds: [], custom: [] };
      state.soundCueGroup = 'custom';
      saveSoundCueSettings();
      renderSoundCueSettings();
    }

    function updateCustomSoundCueType(type) {
      const settings = loadSoundCueSettings();
      const normalized = normalizeSoundCueType(type, settings);
      if (!normalized) return;
      const definition = settings.customTypes?.find(item => item.id === normalized);
      if (!definition) return;
      const nameInput = document.querySelector('[data-sound-custom-name="' + normalized + '"]');
      const descInput = document.querySelector('[data-sound-custom-desc="' + normalized + '"]');
      const name = String(nameInput?.value || '').trim();
      if (!name) {
        const hint = $('hint');
        if (hint) hint.textContent = t('soundCueCustomNamePlaceholder');
        return;
      }
      definition.name = name;
      definition.desc = String(descInput?.value || '').trim();
      definition.aliases = Array.from(new Set([name].concat(Array.isArray(definition.aliases) ? definition.aliases : []).filter(Boolean).map(String)));
      saveSoundCueSettings();
      renderSoundCueSettings();
    }

    function deleteCustomSoundCueType(type) {
      const settings = loadSoundCueSettings();
      const normalized = normalizeSoundCueType(type, settings);
      if (!normalized) return;
      if (!settings.customTypes?.some(item => item.id === normalized)) return;
      if (!confirm(t('soundCueCustomDeleteConfirm'))) return;
      settings.customTypes = settings.customTypes.filter(item => item.id !== normalized);
      delete settings.types[normalized];
      saveSoundCueSettings();
      state.soundCueGroup = 'custom';
      renderSoundCueSettings();
    }

    function soundCueFileExtensionFromMime(mime) {
      const value = String(mime || '').toLowerCase();
      if (value.includes('mpeg')) return '.mp3';
      if (value.includes('wav')) return '.wav';
      if (value.includes('ogg')) return '.ogg';
      if (value.includes('opus')) return '.opus';
      if (value.includes('mp4')) return '.m4a';
      if (value.includes('aac')) return '.aac';
      if (value.includes('flac')) return '.flac';
      if (value.includes('webm')) return '.webm';
      return '.wav';
    }

    function soundCueSafeFileName(name, type, mime, index) {
      const base = String(name || type || 'sound-cue').split(/[\\/]/).pop().replace(/[^a-z0-9._-]+/gi, '-').replace(/^-+|-+$/g, '') || 'sound-cue';
      if (/\.(mp3|wav|ogg|oga|opus|m4a|aac|flac|webm)$/i.test(base)) return base;
      return base + '-' + index + soundCueFileExtensionFromMime(mime);
    }

    function soundCueMimeFromFileName(fileName) {
      const lower = String(fileName || '').toLowerCase();
      if (lower.endsWith('.mp3')) return 'audio/mpeg';
      if (lower.endsWith('.wav')) return 'audio/wav';
      if (lower.endsWith('.ogg') || lower.endsWith('.oga') || lower.endsWith('.opus')) return 'audio/ogg';
      if (lower.endsWith('.m4a')) return 'audio/mp4';
      if (lower.endsWith('.aac')) return 'audio/aac';
      if (lower.endsWith('.flac')) return 'audio/flac';
      if (lower.endsWith('.webm')) return 'audio/webm';
      return 'audio/wav';
    }

    function soundCueZipPathSegment(value) {
      return String(value || 'item').replace(/[^a-z0-9._-]+/gi, '-').replace(/^-+|-+$/g, '') || 'item';
    }

    let soundCueCrcTable = null;
    function soundCueBuildCrcTable() {
      const table = new Uint32Array(256);
      for (let i = 0; i < 256; i++) {
        let value = i;
        for (let bit = 0; bit < 8; bit++) value = (value & 1) ? (0xedb88320 ^ (value >>> 1)) : (value >>> 1);
        table[i] = value >>> 0;
      }
      return table;
    }

    function soundCueCrc32(bytes) {
      soundCueCrcTable = soundCueCrcTable || soundCueBuildCrcTable();
      let crc = 0xffffffff;
      for (let i = 0; i < bytes.length; i++) crc = soundCueCrcTable[(crc ^ bytes[i]) & 0xff] ^ (crc >>> 8);
      return (crc ^ 0xffffffff) >>> 0;
    }

    function soundCueWriteU16(bytes, offset, value) {
      bytes[offset] = value & 0xff;
      bytes[offset + 1] = (value >>> 8) & 0xff;
    }

    function soundCueWriteU32(bytes, offset, value) {
      bytes[offset] = value & 0xff;
      bytes[offset + 1] = (value >>> 8) & 0xff;
      bytes[offset + 2] = (value >>> 16) & 0xff;
      bytes[offset + 3] = (value >>> 24) & 0xff;
    }

    function soundCueReadU16(bytes, offset) {
      return bytes[offset] | (bytes[offset + 1] << 8);
    }

    function soundCueReadU32(bytes, offset) {
      return (bytes[offset] | (bytes[offset + 1] << 8) | (bytes[offset + 2] << 16) | (bytes[offset + 3] << 24)) >>> 0;
    }

    function soundCueDosDateTime(date = new Date()) {
      const year = Math.max(1980, date.getFullYear());
      const time = (date.getHours() << 11) | (date.getMinutes() << 5) | Math.floor(date.getSeconds() / 2);
      const dosDate = ((year - 1980) << 9) | ((date.getMonth() + 1) << 5) | date.getDate();
      return { time, date: dosDate };
    }

    function soundCueNormalizeZipEntryName(name) {
      return String(name || '').replace(/\\/g, '/').replace(/^\/+/, '').split('/').filter(Boolean).join('/');
    }

    async function soundCueBlobToBytes(blob) {
      return new Uint8Array(await blob.arrayBuffer());
    }

    function soundCueCreateZip(entries) {
      const encoder = new TextEncoder();
      const localParts = [];
      const centralParts = [];
      let offset = 0;
      for (const entry of entries) {
        const nameBytes = encoder.encode(soundCueNormalizeZipEntryName(entry.name));
        const data = entry.bytes instanceof Uint8Array ? entry.bytes : new Uint8Array(entry.bytes || []);
        const crc = soundCueCrc32(data);
        const dos = soundCueDosDateTime(entry.date || new Date());
        const local = new Uint8Array(30 + nameBytes.length);
        soundCueWriteU32(local, 0, 0x04034b50);
        soundCueWriteU16(local, 4, 10);
        soundCueWriteU16(local, 6, 0x0800);
        soundCueWriteU16(local, 8, 0);
        soundCueWriteU16(local, 10, dos.time);
        soundCueWriteU16(local, 12, dos.date);
        soundCueWriteU32(local, 14, crc);
        soundCueWriteU32(local, 18, data.length);
        soundCueWriteU32(local, 22, data.length);
        soundCueWriteU16(local, 26, nameBytes.length);
        local.set(nameBytes, 30);
        localParts.push(local, data);

        const central = new Uint8Array(46 + nameBytes.length);
        soundCueWriteU32(central, 0, 0x02014b50);
        soundCueWriteU16(central, 4, 20);
        soundCueWriteU16(central, 6, 10);
        soundCueWriteU16(central, 8, 0x0800);
        soundCueWriteU16(central, 10, 0);
        soundCueWriteU16(central, 12, dos.time);
        soundCueWriteU16(central, 14, dos.date);
        soundCueWriteU32(central, 16, crc);
        soundCueWriteU32(central, 20, data.length);
        soundCueWriteU32(central, 24, data.length);
        soundCueWriteU16(central, 28, nameBytes.length);
        soundCueWriteU32(central, 42, offset);
        central.set(nameBytes, 46);
        centralParts.push(central);
        offset += local.length + data.length;
      }

      const centralOffset = offset;
      const centralSize = centralParts.reduce((sum, part) => sum + part.length, 0);
      const end = new Uint8Array(22);
      soundCueWriteU32(end, 0, 0x06054b50);
      soundCueWriteU16(end, 8, entries.length);
      soundCueWriteU16(end, 10, entries.length);
      soundCueWriteU32(end, 12, centralSize);
      soundCueWriteU32(end, 16, centralOffset);
      return new Blob(localParts.concat(centralParts, [end]), { type: 'application/zip' });
    }

    async function soundCueInflateZipEntry(bytes) {
      if (typeof DecompressionStream !== 'function') throw new Error('Compressed ZIP entries are not supported by this browser.');
      const stream = new Blob([bytes]).stream().pipeThrough(new DecompressionStream('deflate-raw'));
      return new Uint8Array(await new Response(stream).arrayBuffer());
    }

    async function soundCueReadZip(file) {
      const bytes = new Uint8Array(await file.arrayBuffer());
      let eocd = -1;
      for (let i = bytes.length - 22; i >= Math.max(0, bytes.length - 66000); i--) {
        if (soundCueReadU32(bytes, i) === 0x06054b50) { eocd = i; break; }
      }
      if (eocd < 0) throw new Error('Invalid sound cue ZIP package.');
      const total = soundCueReadU16(bytes, eocd + 10);
      let centralOffset = soundCueReadU32(bytes, eocd + 16);
      const decoder = new TextDecoder();
      const entries = new Map();
      for (let i = 0; i < total; i++) {
        if (soundCueReadU32(bytes, centralOffset) !== 0x02014b50) throw new Error('Invalid sound cue ZIP directory.');
        const method = soundCueReadU16(bytes, centralOffset + 10);
        const compressedSize = soundCueReadU32(bytes, centralOffset + 20);
        const nameLength = soundCueReadU16(bytes, centralOffset + 28);
        const extraLength = soundCueReadU16(bytes, centralOffset + 30);
        const commentLength = soundCueReadU16(bytes, centralOffset + 32);
        const localOffset = soundCueReadU32(bytes, centralOffset + 42);
        const name = soundCueNormalizeZipEntryName(decoder.decode(bytes.slice(centralOffset + 46, centralOffset + 46 + nameLength)));
        if (soundCueReadU32(bytes, localOffset) !== 0x04034b50) throw new Error('Invalid sound cue ZIP entry.');
        const localNameLength = soundCueReadU16(bytes, localOffset + 26);
        const localExtraLength = soundCueReadU16(bytes, localOffset + 28);
        const dataStart = localOffset + 30 + localNameLength + localExtraLength;
        const compressed = bytes.slice(dataStart, dataStart + compressedSize);
        const data = method === 0 ? compressed : method === 8 ? await soundCueInflateZipEntry(compressed) : null;
        if (data) entries.set(name, data);
        centralOffset += 46 + nameLength + extraLength + commentLength;
      }
      return entries;
    }

    async function dataUrlToSoundCueFile(dataUrl, name, type, index) {
      const res = await fetch(dataUrl);
      if (!res.ok) throw new Error('Failed to read embedded sound cue audio.');
      const blob = await res.blob();
      if (blob.size > 8 * 1024 * 1024) throw new Error('Embedded sound cue audio is too large.');
      const fileName = soundCueSafeFileName(name, type, blob.type, index);
      return new File([blob], fileName, { type: blob.type || 'audio/wav' });
    }

    async function uploadSoundCueFileToServer(type, file) {
      if (!type || !file) return null;
      if (!state.agent) {
        throw new Error(t('soundCueUploadNoAgent'));
      }
      const form = new FormData();
      form.append('agent', state.agent);
      form.append('type', type);
      form.append('audio', file);
      return await api('/api/sound-cue', { method: 'POST', body: form });
    }

    async function uploadSoundCueFileToType(type, file) {
      const data = await uploadSoundCueFileToServer(type, file);
      if (!data) return null;
      const settings = loadSoundCueSettings();
      settings.types[type] = settings.types[type] || { enabled: true, disabledItemIds: [], custom: [] };
      settings.types[type].custom = settings.types[type].custom || [];
      const itemId = 'custom:' + type + ':' + Date.now() + ':' + Math.random().toString(36).slice(2, 8);
      settings.types[type].custom.push({
        id: itemId,
        name: data.name || file.name || type,
        url: data.url,
        relativePath: data.relativePath || null,
        source: 'custom',
        enabled: true
      });
      ensureSoundCueTypeHasEnabledItem(settings, type);
      return itemId;
    }

    async function uploadSoundCueFiles(files) {
      const settings = loadSoundCueSettings();
      const type = normalizeSoundCueType(state.soundCueUploadType, settings);
      state.soundCueUploadType = null;
      const list = Array.from(files || []).filter(Boolean);
      if (!type || !list.length) return;
      let firstItemId = null;
      for (const file of list) {
        const itemId = await uploadSoundCueFileToType(type, file);
        if (!firstItemId) firstItemId = itemId;
      }
      saveSoundCueSettings();
      renderSoundCueSettings();
      if (firstItemId) playSoundCue(type, { force: true, itemId: firstItemId });
    }

    function cloneSoundCueSettings(settings) {
      return JSON.parse(JSON.stringify(settings || loadSoundCueSettings()));
    }

    function blobToDataUrl(blob) {
      return new Promise(function(resolve, reject) {
        const reader = new FileReader();
        reader.onload = function() { resolve(String(reader.result || '')); };
        reader.onerror = function() { reject(reader.error || new Error('Failed to read audio asset.')); };
        reader.readAsDataURL(blob);
      });
    }

    async function exportSoundCueSettings() {
      const bundleSettings = cloneSoundCueSettings(loadSoundCueSettings());
      const zipEntries = [];
      let exportedAssets = 0;
      for (const [typeId, config] of Object.entries(bundleSettings.types || {})) {
        const nextCustom = [];
        const custom = Array.isArray(config.custom) ? config.custom : [];
        for (let index = 0; index < custom.length; index++) {
          const item = custom[index];
          const rawUrl = item?.dataUrl || item?.url;
          if (!rawUrl) continue;
          try {
            const res = await fetch(rawUrl);
            if (!res.ok) continue;
            const blob = await res.blob();
            if (blob.size > 8 * 1024 * 1024) continue;
            const fileName = soundCueSafeFileName(item.name || item.fileName || typeId, typeId, blob.type, index);
            const assetPath = 'audio/' + soundCueZipPathSegment(typeId) + '/' + Date.now().toString(36) + '-' + index + '-' + soundCueZipPathSegment(fileName);
            zipEntries.push({ name: assetPath, bytes: await soundCueBlobToBytes(blob) });
            nextCustom.push({
              id: item.id,
              name: String(item.name || item.fileName || typeId),
              fileName,
              assetPath,
              mimeType: blob.type || soundCueMimeFromFileName(fileName),
              size: blob.size,
              source: 'custom',
              enabled: item.enabled !== false
            });
            exportedAssets++;
          } catch {}
        }
        config.custom = nextCustom;
      }
      const payload = {
        packageType: 'matdance-sound-cues',
        version: 2,
        exportedAt: new Date().toISOString(),
        format: 'zip-assets',
        soundCues: bundleSettings
      };
      zipEntries.unshift({ name: 'manifest.json', bytes: new TextEncoder().encode(JSON.stringify(payload, null, 2)) });
      zipEntries.push({ name: 'README.txt', bytes: new TextEncoder().encode('Matdance sound cue package. Audio assets are stored under audio/ and manifest.json describes sound cue settings. Local runtime URLs are intentionally not exported.\n') });
      const blob = soundCueCreateZip(zipEntries);
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = 'matdance-sound-cues-' + new Date().toISOString().slice(0, 10) + '.zip';
      document.body.appendChild(link);
      link.click();
      link.remove();
      setTimeout(function() { URL.revokeObjectURL(url); }, 1000);
      const hint = $('hint');
      if (hint) hint.textContent = t('soundCueExportDone') + ' (' + exportedAssets + ' audio)';
    }

    async function persistImportedSoundCueItem(typeId, item, index, packageEntries) {
      const assetPath = soundCueNormalizeZipEntryName(item?.assetPath || item?.packagePath || item?.audioPath || '');
      if (assetPath) {
        const bytes = packageEntries?.get(assetPath);
        if (!bytes) throw new Error('Missing packaged audio asset: ' + assetPath);
        const fileName = soundCueSafeFileName(item?.fileName || item?.name || assetPath.split('/').pop(), typeId, item?.mimeType, index);
        const file = new File([bytes], fileName, { type: item?.mimeType || soundCueMimeFromFileName(fileName) });
        const data = await uploadSoundCueFileToServer(typeId, file);
        if (!data) return null;
        return {
          name: data.name || item?.name || fileName,
          url: data.url,
          relativePath: data.relativePath || null
        };
      }
      const rawUrl = item?.dataUrl || item?.url;
      if (!rawUrl) return null;
      const sourceName = String(item?.name || item?.fileName || typeId);
      if (String(rawUrl).startsWith('data:')) {
        const file = await dataUrlToSoundCueFile(String(rawUrl), sourceName, typeId, index);
        const data = await uploadSoundCueFileToServer(typeId, file);
        if (!data) return null;
        return {
          name: data.name || sourceName,
          url: data.url,
          relativePath: data.relativePath || null
        };
      }
      return {
        name: sourceName,
        url: String(rawUrl),
        relativePath: item?.relativePath || null
      };
    }

    async function mergeCustomSoundCueItems(targetConfig, sourceConfig, typeId, packageEntries) {
      if (!sourceConfig) return;
      const target = targetConfig.custom = Array.isArray(targetConfig.custom) ? targetConfig.custom : [];
      const disabled = new Set(Array.isArray(targetConfig.disabledItemIds) ? targetConfig.disabledItemIds : []);
      const sourceDisabled = new Set(Array.isArray(sourceConfig.disabledItemIds) ? sourceConfig.disabledItemIds.filter(Boolean).map(String) : []);
      const known = new Set(target.map(item => (item.id || '') + '|' + (item.url || item.dataUrl || '')));
      const items = Array.isArray(sourceConfig.custom) ? sourceConfig.custom : [];
      for (let index = 0; index < items.length; index++) {
        const item = items[index];
        const sourceId = String(item?.id || '');
        if (sourceId && target.some(existing => existing.id === sourceId)) continue;
        const persisted = await persistImportedSoundCueItem(typeId, item, index, packageEntries);
        if (!persisted?.url) continue;
        let id = sourceId || ('custom:' + typeId + ':' + Date.now() + ':' + index);
        if (target.some(existing => existing.id === id)) id = 'custom:' + typeId + ':' + Date.now() + ':' + index;
        const key = id + '|' + persisted.url;
        if (known.has(key)) continue;
        known.add(key);
        if (item?.enabled === false || sourceDisabled.has(sourceId)) disabled.add(id);
        target.push({
          id,
          name: persisted.name,
          url: persisted.url,
          relativePath: persisted.relativePath || null,
          source: 'custom',
          enabled: item?.enabled !== false
        });
      }
      targetConfig.disabledItemIds = Array.from(disabled);
    }

    async function importSoundCueSettingsPayload(payload, packageEntries) {
      const source = payload?.soundCues || payload?.settings || payload;
      if (!source || typeof source !== 'object') throw new Error('Invalid sound cue bundle.');
      const settings = loadSoundCueSettings();
      if (typeof source.enabled === 'boolean') settings.enabled = source.enabled;
      if (Number.isFinite(Number(source.volume))) settings.volume = Math.max(0, Math.min(1, Number(source.volume)));
      if (Number.isFinite(Number(source.delayMs))) settings.delayMs = Math.max(0, Math.min(30000, Number(source.delayMs)));
      for (const type of SOUND_CUE_TYPES) {
        const saved = source.types?.[type.id];
        if (!saved) continue;
        const config = settings.types[type.id] = settings.types[type.id] || { enabled: true, disabledItemIds: [], custom: [] };
        if (typeof saved.enabled === 'boolean') config.enabled = saved.enabled;
        config.disabledItemIds = Array.isArray(saved.disabledItemIds) ? saved.disabledItemIds.filter(Boolean).map(String) : (config.disabledItemIds || []);
        await mergeCustomSoundCueItems(config, saved, type.id, packageEntries);
        ensureSoundCueTypeHasEnabledItem(settings, type.id);
      }
      for (const item of (Array.isArray(source.customTypes) ? source.customTypes : [])) {
        const name = String(item?.name || '').trim();
        if (!name) continue;
        let definition = settings.customTypes?.find(type => type.id === item.id || soundCueToken(type.name).compact === soundCueToken(name).compact);
        if (!definition) {
          const id = uniqueCustomSoundCueId(name, settings, item.id);
          definition = { id, name, desc: String(item.desc || ''), aliases: [name], custom: true };
          settings.customTypes = settings.customTypes || [];
          settings.customTypes.push(definition);
          settings.types[id] = settings.types[id] || { enabled: true, disabledItemIds: [], custom: [] };
        } else {
          definition.name = name;
          definition.desc = String(item.desc || definition.desc || '');
          definition.aliases = Array.from(new Set([name].concat(Array.isArray(item.aliases) ? item.aliases : [], Array.isArray(definition.aliases) ? definition.aliases : []).filter(Boolean).map(String)));
        }
        const saved = source.types?.[item.id] || source.types?.[definition.id];
        const config = settings.types[definition.id] = settings.types[definition.id] || { enabled: true, disabledItemIds: [], custom: [] };
        if (saved) {
          if (typeof saved.enabled === 'boolean') config.enabled = saved.enabled;
          config.disabledItemIds = Array.isArray(saved.disabledItemIds) ? saved.disabledItemIds.filter(Boolean).map(String) : (config.disabledItemIds || []);
          await mergeCustomSoundCueItems(config, saved, definition.id, packageEntries);
        }
        ensureSoundCueTypeHasEnabledItem(settings, definition.id);
      }
      state.soundCueGroup = 'custom';
      saveSoundCueSettings();
      renderSoundCueSettings();
      const hint = $('hint');
      if (hint) hint.textContent = t('soundCueImportDone');
    }

    async function importSoundCueSettingsFile(file) {
      if (!file) return;
      const isZip = /\.zip$/i.test(file.name || '') || String(file.type || '').includes('zip');
      if (isZip) {
        const entries = await soundCueReadZip(file);
        const manifest = entries.get('manifest.json') || entries.get('sound-cues.json');
        if (!manifest) throw new Error('Sound cue ZIP package is missing manifest.json.');
        const text = new TextDecoder().decode(manifest);
        await importSoundCueSettingsPayload(JSON.parse(text), entries);
        return;
      }
      const text = await file.text();
      await importSoundCueSettingsPayload(JSON.parse(text), null);
    }

    function micIconSvg() {
      return '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 2a3 3 0 0 0-3 3v7a3 3 0 0 0 6 0V5a3 3 0 0 0-3-3Z"></path><path d="M19 10v2a7 7 0 0 1-14 0v-2"></path><path d="M12 19v3"></path></svg>';
    }

    function speakerIconSvg() {
      return '<svg class="speaker-icon" viewBox="0 0 24 24" aria-hidden="true"><path d="M11 5 6 9H2v6h4l5 4V5Z"></path><path d="M15.54 8.46a5 5 0 0 1 0 7.07"></path><path d="M19.07 4.93a10 10 0 0 1 0 14.14"></path></svg>';
    }

    function voiceHoldContent() {
      return '<span class="voice-hold-icon" aria-hidden="true">' + micIconSvg() + '</span><span class="voice-hold-copy"><span class="voice-hold-title">' + escapeHtml(t('voiceHold')) + '</span><small>' + escapeHtml(t('voiceHoldHint')) + '</small></span>';
    }

    function updateVoiceUi() {
      const mic = $('micButton');
      const input = $('input');
      const hold = $('voiceHold');
      const box = document.querySelector('.composer-box');
      const canStt = canUseStt();
      if (mic) {
        mic.hidden = !canStt;
        mic.disabled = state.busy || !canStt;
        mic.classList.toggle('active', state.voiceMode);
        mic.title = t('voiceInput');
        mic.setAttribute('aria-label', t('voiceInput'));
        mic.setAttribute('aria-pressed', state.voiceMode ? 'true' : 'false');
      }
      if (box) box.classList.toggle('voice-mode', state.voiceMode && canStt);
      if (input) input.hidden = state.voiceMode && canStt;
      if (hold) {
        hold.hidden = !(state.voiceMode && canStt);
        hold.disabled = state.busy || !canStt;
        hold.classList.toggle('recording', !!state.voiceRecording);
        hold.innerHTML = voiceHoldContent();
      }
      if (!canStt && state.voiceMode) state.voiceMode = false;
    }

    function setVoiceMode(enabled) {
      if (enabled && (state.busy || !canUseStt())) return;
      if (!enabled && state.voiceRecording) return;
      state.voiceMode = !!enabled;
      updateVoiceUi();
      if (state.voiceMode) $('voiceHold')?.focus();
      else $('input')?.focus();
    }

    function attachAudioControl(meta, content, messageIndex, audio, options = {}) {
      const spoken = speechText(content);
      if (!meta || (!audio && (!canUseTts() || !spoken))) return null;
      const button = document.createElement('button');
      button.type = 'button';
      button.className = 'ghost audio-button speaker-button';
      button.title = audio ? t('ttsPlayGenerated') : t('ttsGenerateForMessage');
      button.setAttribute('aria-label', button.title);
      button.innerHTML = speakerIconSvg();
      let targetAudio = audio || null;
      const setLoading = function(loading) {
        button.dataset.loading = loading ? '1' : '';
        button.classList.toggle('loading', !!loading);
        button.setAttribute('aria-busy', loading ? 'true' : 'false');
      };
      const setReady = function(nextAudio) {
        setLoading(false);
        if (!nextAudio?.url) {
          button.title = t('ttsGenerateForMessage');
          button.setAttribute('aria-label', button.title);
          return;
        }

        targetAudio = nextAudio;
        button.title = t('ttsPlayGenerated');
        button.setAttribute('aria-label', button.title);
        button.classList.add('ready-pulse');
        setTimeout(function() { button.classList.remove('ready-pulse'); }, 520);
      };
      button.addEventListener('click', async function() {
        try {
          if (button.dataset.loading === '1') return;
          if (!targetAudio) {
            setLoading(true);
            setReady(await requestSpeech(content, messageIndex));
          }
          if (!targetAudio) return;
          playAudio(targetAudio, button, { showError: true });
        } catch (err) {
          setLoading(false);
          const message = err.message || String(err);
          button.title = message;
          button.setAttribute('aria-label', button.title);
          showTtsErrorOverlay(message);
        } finally {
          if (targetAudio) button.setAttribute('aria-busy', 'false');
        }
      });
      meta.appendChild(button);
      if (options.loading) setLoading(true);
      if (audio) setReady(audio);
      return { button, setLoading, setReady, getAudio: () => targetAudio };
    }

    async function requestSpeech(text, messageIndex) {
      const spoken = speechText(text);
      if (!spoken) throw new Error(t('ttsNoSpeakableText'));
      const data = await api('/api/audio/speech', {
        method: 'POST',
        headers: { 'content-type': 'application/json' },
        body: JSON.stringify({
          agent: state.agent,
          session: state.session,
          messageIndex: Number.isInteger(messageIndex) && messageIndex >= 0 ? messageIndex : null,
          text: spoken
        })
      });
      if (!data.audio?.url) throw new Error(t('ttsNoPlayableAudio'));
      return data.audio;
    }

    function speechText(text) {
      return stripPlayAudioMarkers(String(text || '').replace(/\{show_file:[^}]+\}/g, '').replace(/\[preview:[^\]]+\]/g, '')).trim();
    }

    function showTtsErrorOverlay(message) {
      const overlay = $('ttsErrorOverlay');
      const body = $('ttsErrorMessage');
      if (!overlay || !body) return;
      body.textContent = String(message || t('ttsErrorFallback'));
      overlay.classList.add('active');
      overlay.setAttribute('aria-hidden', 'false');
      if (state.ttsErrorTimer) clearTimeout(state.ttsErrorTimer);
      state.ttsErrorTimer = setTimeout(hideTtsErrorOverlay, 5000);
    }

    function hideTtsErrorOverlay() {
      const overlay = $('ttsErrorOverlay');
      if (!overlay) return;
      overlay.classList.remove('active');
      overlay.setAttribute('aria-hidden', 'true');
      if (state.ttsErrorTimer) {
        clearTimeout(state.ttsErrorTimer);
        state.ttsErrorTimer = null;
      }
    }

    function playAudio(audio, button, options = {}) {
      if (!audio?.url) return;
      const showError = options.showError !== false;
      if (state.audioPlayer && state.audioUrl === audio.url) {
        if (!state.audioPlayer.paused) {
          state.audioPlayer.pause();
          return;
        }

        if (state.audioButton && state.audioButton !== button) state.audioButton.classList.remove('active');
        state.audioButton = button || state.audioButton;
        button?.classList.add('active');
        if (state.audioPlayer.ended) {
          try { state.audioPlayer.currentTime = 0; } catch {}
        }
        const player = state.audioPlayer;
        state.audioPlayer.play().catch(function(err) {
          if (state.audioPlayer === player) {
            if (state.audioButton) state.audioButton.classList.remove('active');
            state.audioPlayer = null;
            state.audioUrl = null;
            state.audioButton = null;
          }
          const message = err.message || String(err);
          if (button) button.title = message;
          if (showError) showTtsErrorOverlay(message);
        });
        return;
      }
      if (state.audioPlayer) {
        try { state.audioPlayer.pause(); } catch {}
      }
      if (state.audioButton) state.audioButton.classList.remove('active');
      const player = new Audio(audio.url);
      state.audioPlayer = player;
      state.audioUrl = audio.url;
      state.audioButton = button || null;
      button?.classList.add('active');
      const clearCurrent = function() {
        if (state.audioPlayer !== player) return;
        if (state.audioButton) state.audioButton.classList.remove('active');
        state.audioPlayer = null;
        state.audioUrl = null;
        state.audioButton = null;
      };
      player.onended = clearCurrent;
      player.onpause = clearCurrent;
      player.play().catch(function(err) {
        clearCurrent();
        const message = err.message || String(err);
        if (button) button.title = message;
        if (showError) showTtsErrorOverlay(message);
      });
    }

    function prepareSpeechAfterStream(ai, text, messageIndex, audio) {
      const tts = effectiveTts();
      const spoken = speechText(text);
      if (!tts || tts.mode === 'off' || !hasEffectiveKey(tts) || !spoken) return null;
      let result = audio || null;
      const shouldGenerate = !result && tts.mode === 'chat_visible_only' && messageIndex >= 0 && !!tts.autoPlay;
      const control = attachAudioControl(ai.meta, text, messageIndex, result, { loading: shouldGenerate });
      if (!control) return null;
      const task = {
        control,
        autoPlay: !!tts.autoPlay,
        promise: Promise.resolve(result)
      };
      if (shouldGenerate) {
        task.promise = requestSpeech(text, messageIndex)
          .then(function(nextAudio) {
            result = nextAudio;
            control.setReady(result);
            return result;
          })
          .catch(function(err) {
            control.setLoading(false);
            const message = err.message || String(err);
            control.button.title = message;
            showTtsErrorOverlay(message);
            return null;
          });
      } else if (result) {
        control.setReady(result);
      }
      ai.ttsTask = task;
      return task;
    }

    async function maybeAutoPlayPreparedSpeech(task) {
      if (!task?.autoPlay) return;
      const result = await task.promise;
      if (result?.url) playAudio(result, task.control?.button, { showError: false });
    }

    async function maybeGenerateSpeechAfterStream(ai, text, messageIndex, audio) {
      const task = prepareSpeechAfterStream(ai, text, messageIndex, audio);
      await maybeAutoPlayPreparedSpeech(task);
    }

    async function transcribeBlob(blob, fileName) {
      throw new Error('File transcription is not available in browser Microsoft/Web Speech mode. Use Record.');
    }

    function recognizeSpeechOnce(assignRecognition) {
      const Ctor = speechRecognitionCtor();
      if (!Ctor) return Promise.reject(new Error('Browser speech recognition is unavailable.'));
      return new Promise(function(resolve, reject) {
        const recognition = new Ctor();
        let transcript = '';
        recognition.lang = state.lang === 'zh' ? 'zh-CN' : (navigator.language || 'en-US');
        recognition.continuous = true;
        recognition.interimResults = true;
        recognition.onresult = function(event) {
          transcript = '';
          for (let i = 0; i < event.results.length; i++) {
            transcript += event.results[i][0]?.transcript || '';
          }
        };
        recognition.onerror = function(event) {
          reject(new Error(event.error || 'speech recognition failed'));
        };
        recognition.onend = function() {
          resolve(transcript.trim());
        };
        assignRecognition(recognition);
        recognition.start();
      });
    }

    function createSpeechRecognitionSession() {
      const Ctor = speechRecognitionCtor();
      if (!Ctor) throw new Error('Browser speech recognition is unavailable.');
      const session = { recognition: null, canceled: false, transcript: '', promise: null };
      session.promise = new Promise(function(resolve, reject) {
        const recognition = new Ctor();
        session.recognition = recognition;
        recognition.lang = state.lang === 'zh' ? 'zh-CN' : (navigator.language || 'en-US');
        recognition.continuous = true;
        recognition.interimResults = true;
        recognition.onresult = function(event) {
          session.transcript = '';
          for (let i = 0; i < event.results.length; i++) {
            session.transcript += event.results[i][0]?.transcript || '';
          }
        };
        recognition.onerror = function(event) {
          if (session.canceled) return;
          reject(new Error(event.error || 'speech recognition failed'));
        };
        recognition.onend = function() {
          resolve(session.transcript.trim());
        };
        recognition.start();
      });
      session.stop = function() {
        try { session.recognition?.stop(); } catch {}
      };
      session.abort = function() {
        session.canceled = true;
        try {
          if (session.recognition?.abort) session.recognition.abort();
          else session.recognition?.stop();
        } catch {}
      };
      return session;
    }

    function updateVoiceOverlay(cancel = state.voiceCanceled, label) {
      const overlay = $('voiceRecordOverlay');
      const time = $('voiceRecordTime');
      const hint = $('voiceRecordHint');
      if (!overlay) return;
      overlay.classList.toggle('cancel', !!cancel);
      if (time && state.voiceStartAt) {
        time.textContent = ((Date.now() - state.voiceStartAt) / 1000).toFixed(1) + 's';
      }
      if (hint) hint.textContent = label || (cancel ? t('voiceSlideCancel') : t('voiceReleaseSend'));
    }

    function startVoiceHold(event) {
      if (state.busy || state.voiceRecording || !canUseStt()) return;
      event.preventDefault();
      const hold = $('voiceHold');
      try { hold?.setPointerCapture?.(event.pointerId); } catch {}
      state.voicePointerId = event.pointerId;
      state.voiceRecording = true;
      state.voiceCanceled = false;
      state.voiceStartY = event.clientY || 0;
      state.voiceStartAt = Date.now();
      try {
        state.voiceSession = createSpeechRecognitionSession();
      } catch (err) {
        state.voiceRecording = false;
        $('hint').textContent = t('sttFailedPrefix') + ': ' + (err.message || String(err));
        return;
      }
      $('voiceRecordOverlay')?.classList.add('active');
      updateVoiceOverlay(false);
      state.voiceTimer = setInterval(function() { updateVoiceOverlay(); }, 100);
      updateVoiceUi();
    }

    function moveVoiceHold(event) {
      if (!state.voiceRecording || event.pointerId !== state.voicePointerId) return;
      const y = event.clientY || 0;
      state.voiceCanceled = y && state.voiceStartY ? (y - state.voiceStartY) < -72 : false;
      updateVoiceOverlay();
    }

    async function finishVoiceHold(event, forceCancel = false) {
      if (!state.voiceRecording || (event?.pointerId != null && event.pointerId !== state.voicePointerId)) return;
      event?.preventDefault?.();
      const canceled = forceCancel || state.voiceCanceled;
      const session = state.voiceSession;
      state.voiceRecording = false;
      state.voicePointerId = null;
      if (state.voiceTimer) clearInterval(state.voiceTimer);
      state.voiceTimer = null;
      updateVoiceUi();
      if (!session) return;
      if (canceled) {
        session.abort();
        $('voiceRecordOverlay')?.classList.remove('active');
        $('hint').textContent = t('voiceCanceled');
        state.voiceSession = null;
        return;
      }

      updateVoiceOverlay(false, t('voiceTranscribing'));
      session.stop();
      try {
        const text = await session.promise;
        $('voiceRecordOverlay')?.classList.remove('active');
        state.voiceSession = null;
        if (!text.trim()) {
          $('hint').textContent = t('voiceNoSpeech');
          return;
        }
        const input = $('input');
        input.value = input.value.trim() ? input.value.trim() + '\n' + text : text;
        setVoiceMode(false);
        input.focus();
        if (effectiveStt()?.sendAfterTranscription && !state.busy) $('composer').requestSubmit();
      } catch (err) {
        $('voiceRecordOverlay')?.classList.remove('active');
        state.voiceSession = null;
        $('hint').textContent = t('sttFailedPrefix') + ': ' + (err.message || String(err));
      }
    }

    function isPreviewFileContent(content) {
      return typeof content === 'string' && content.includes('[preview_file]');
    }

    function renderPreviewFiles(content) {
      const lines = content.split('\n').filter(l => l.trim().startsWith('- [preview]'));
      if (!lines.length) return null;

      const container = document.createElement('div');
      container.className = 'preview-file-container';
      container.style.cssText = 'display:flex;flex-direction:column;gap:12px;margin:8px 0;';

      lines.forEach(line => {
        const match = line.match(/- \[preview\] (.+?) \| type=(\w+) \| size=(\d+) bytes \| path=(.+)/);
        if (!match) return;

        const [, filePath, fileType, size, fullPath] = match;
        const previewWrap = document.createElement('div');
        previewWrap.className = 'preview-file-item';
        previewWrap.style.cssText = 'border:1px solid var(--line);border-radius:12px;overflow:hidden;background:var(--panel-solid);';

        const header = document.createElement('div');
        header.style.cssText = 'padding:8px 12px;background:rgba(255,255,255,.04);font-size:12px;color:var(--soft);display:flex;justify-content:space-between;align-items:center;';
        const mapType = (t) => ({image:'image',html:'html',markdown:'markdown',text:'text',code:'text',pdf:'document'})[t] || 'unknown';
        header.innerHTML = `<span>${escapeHtml(filePath)}</span><span style="color:var(--faint);display:flex;align-items:center;gap:8px;">${fileType} · ${formatBytes(size)} <button type="button" class="open-browser-btn" style="background:none;border:none;color:var(--cyan);cursor:pointer;font-size:13px;padding:2px 6px;border-radius:4px;" title="${escapeHtml(t('previewOpenFull'))}">↗</button></span>`;
        header.querySelector('.open-browser-btn').onclick = () => openFileBrowser(fullPath, filePath, mapType(fileType));
        previewWrap.appendChild(header);

        const body = document.createElement('div');
        body.style.cssText = 'padding:12px;min-height:60px;max-height:400px;overflow:auto;';

        if (fileType === 'image') {
          const img = document.createElement('img');
          img.src = previewFileUrl(fullPath);
          img.style.cssText = 'max-width:100%;border-radius:8px;display:block;';
          img.onerror = () => { img.style.display='none'; body.innerHTML = '<div style="color:var(--faint)">' + escapeHtml(t('previewLoadImageFailed')) + '</div>'; };
          body.appendChild(img);
        } else if (fileType === 'html') {
          const iframe = document.createElement('iframe');
          iframe.src = previewFileUrl(fullPath);
          iframe.style.cssText = 'width:100%;height:300px;border:none;border-radius:8px;background:#fff;';
          body.appendChild(iframe);
        } else if (fileType === 'markdown') {
          fetch(previewFileUrl(fullPath))
            .then(ensurePreviewResponse)
            .then(r => r.text())
            .then(text => {
              const md = document.createElement('div');
              md.className = 'markdown';
              setMarkdownContent(md, text);
              body.appendChild(md);
            })
            .catch(() => { body.innerHTML = '<div style="color:var(--faint)">' + escapeHtml(t('previewLoadMarkdownFailed')) + '</div>'; });
        } else if (fileType === 'text' || fileType === 'code') {
          fetch(previewFileUrl(fullPath))
            .then(ensurePreviewResponse)
            .then(r => r.text())
            .then(text => {
              const pre = document.createElement('pre');
              pre.style.cssText = 'margin:0;padding:12px;background:rgba(0,0,0,.3);border-radius:8px;overflow:auto;font-size:12px;line-height:1.5;';
              const code = document.createElement('code');
              code.textContent = text.substring(0, 2000) + (text.length > 2000 ? '\n...[truncated]' : '');
              pre.appendChild(code);
              body.appendChild(pre);
            })
            .catch(() => { body.innerHTML = '<div style="color:var(--faint)">' + escapeHtml(t('previewLoadFileFailed')) + '</div>'; });
        } else {
          body.innerHTML = `<div style="color:var(--faint);padding:8px;">${escapeHtml(t('previewUnavailable'))} <a href="${previewFileUrl(fullPath)}" target="_blank" style="color:var(--cyan)">${escapeHtml(t('previewDownload'))}</a></div>`;
        }

        previewWrap.appendChild(body);
        container.appendChild(previewWrap);
      });

      return container;
    }

    function escapeHtml(text) {
      const div = document.createElement('div');
      div.textContent = text;
      return div.innerHTML;
    }

    function formatBytes(bytes) {
      const n = parseInt(bytes, 10);
      if (n < 1024) return n + ' B';
      if (n < 1024 * 1024) return (n / 1024).toFixed(1) + ' KB';
      return (n / (1024 * 1024)).toFixed(1) + ' MB';
    }

    function renderStreamMarkdownSegment(segment, parsePreviews = false) {
      const node = segment?.node || segment?.body;
      if (!node) return;
      const buffer = String(segment.buffer || '');
      if (!parsePreviews && segment.renderedBuffer === buffer) return;
      renderMarkdownContent(node, buffer, parsePreviews);
      segment.renderedBuffer = buffer;
    }

    function scheduleStreamMarkdownSegment(segment, afterRender) {
      if (!segment || segment.renderScheduled) return;
      segment.renderScheduled = true;
      requestAnimationFrame(function() {
        segment.renderScheduled = false;
        renderStreamMarkdownSegment(segment, false);
        if (typeof afterRender === 'function') afterRender();
      });
    }

    function flushStreamMarkdownSegment(segment, parsePreviews = false) {
      if (!segment) return;
      segment.renderScheduled = false;
      renderStreamMarkdownSegment(segment, parsePreviews);
    }

    function renderPlainTextSegment(segment) {
      const node = segment?.node || segment?.raw;
      if (!node) return;
      const buffer = String(segment.buffer || '');
      if (segment.renderedBuffer === buffer) return;
      node.textContent = buffer;
      node.hidden = !buffer.trim();
      segment.renderedBuffer = buffer;
    }

    function schedulePlainTextSegment(segment, afterRender) {
      if (!segment || segment.renderScheduled) return;
      segment.renderScheduled = true;
      requestAnimationFrame(function() {
        segment.renderScheduled = false;
        renderPlainTextSegment(segment);
        if (typeof afterRender === 'function') afterRender();
      });
    }

    function flushPlainTextSegment(segment) {
      if (!segment) return;
      segment.renderScheduled = false;
      renderPlainTextSegment(segment);
    }

    function addAssistantStreaming() {
      const msg = addMessage('assistant', '', assistantMeta(t('streaming')), { deferAudioControl: true });
      const loader = document.createElement('span');
      loader.className = 'matrix';
      loader.textContent = frames[0];
      const text = document.createElement('div');
      text.className = 'stream-text markdown';
      const streamLine = document.createElement('div');
      streamLine.className = 'stream-line';
      msg.card.innerHTML = '';
      streamLine.append(loader, text);
      msg.card.appendChild(streamLine);
      const toolGroup = createToolGroup(msg.card);
      startMatrix(loader);
      const firstSegment = { line: streamLine, node: text, buffer: '' };
      return { ...msg, loader, text, toolGroup, buffer: '', textSegments: [firstSegment], currentTextSegment: firstSegment, soundCueSegments: [], currentSoundCueSegment: null, soundCueCount: 0 };
    }

    function ensureAssistantTextSegment(ai) {
      if (ai.currentTextSegment) return ai.currentTextSegment;
      const text = document.createElement('div');
      text.className = 'stream-text markdown';
      const streamLine = document.createElement('div');
      streamLine.className = 'stream-line';
      const spacer = document.createElement('span');
      spacer.className = 'stream-spacer';
      streamLine.append(spacer, text);
      ai.card.appendChild(streamLine);
      const segment = { line: streamLine, node: text, buffer: '' };
      ai.textSegments.push(segment);
      ai.currentTextSegment = segment;
      return segment;
    }

    function appendAssistantStreamText(ai, text) {
      if (!text) return;
      if (ai.currentSoundCueSegment) {
        const cueSegment = ai.currentSoundCueSegment;
        cueSegment.buffer += String(text || '');
        cueSegment.body.hidden = !stripPlayAudioMarkers(cueSegment.buffer).trim();
        scheduleStreamMarkdownSegment(cueSegment, function() { scheduleScrollBottom({ stream: true }); });
        return;
      }
      const segment = ensureAssistantTextSegment(ai);
      segment.buffer += String(text || '');
      scheduleStreamMarkdownSegment(segment, function() { scheduleScrollBottom({ stream: true }); });
    }

    function appendAssistantSoundCue(ai, cue) {
      const block = createSoundCueEvent(cue, 'reply');
      ai.card.appendChild(block);
      ai.soundCueCount = (ai.soundCueCount || 0) + 1;
      const segment = { block, body: soundCueEventBody(block), buffer: '' };
      ai.soundCueSegments.push(segment);
      ai.currentSoundCueSegment = segment;
      ai.currentTextSegment = null;
      scheduleScrollBottom({ stream: true });
      return block;
    }

    function finalizeAssistantTextSegments(ai) {
      (ai.textSegments || []).forEach(function(segment) {
        flushStreamMarkdownSegment(segment, true);
      });
      (ai.soundCueSegments || []).forEach(function(segment) {
        flushStreamMarkdownSegment(segment, true);
        segment.body.hidden = !stripPlayAudioMarkers(segment.buffer).trim();
      });
    }

    function addThinkingCard(content = '', beforeNode = null, options = {}) {
      const rawContent = String(content || '');
      const msg = addMessage('assistant', '', assistantMeta(t('thinkingBlock')), { deferAudioControl: true, target: options.target, scroll: options.scroll });
      msg.wrap.classList.add('thinking-message');
      msg.card.className = 'thinking-card';
      msg.card.innerHTML = '';
      if (beforeNode && beforeNode.parentNode) {
        beforeNode.parentNode.insertBefore(msg.wrap, beforeNode);
      }
      msg.wrap.hidden = !rawContent.trim();
      const thinking = { ...msg, segments: [], currentSegment: null, soundCueSegments: [], currentSoundCueSegment: null, buffer: '', soundCueCount: 0 };
      if (rawContent) {
        appendThinking(thinking, rawContent, beforeNode, { scroll: options.scroll });
      }
      return thinking;
    }

    function ensureThinkingTextSegment(thinking) {
      if (thinking.currentSegment) return thinking.currentSegment;
      const raw = document.createElement('pre');
      raw.className = 'thinking-raw';
      thinking.card.appendChild(raw);
      const segment = { node: raw, buffer: '' };
      thinking.segments.push(segment);
      thinking.currentSegment = segment;
      return segment;
    }

    function appendThinking(thinking, text, beforeNode = null, options = {}) {
      if (!text) return thinking;
      const node = thinking || addThinkingCard('', beforeNode);
      const rawText = String(text || '');
      node.buffer += rawText;
      if (node.currentSoundCueSegment) {
        const cueSegment = node.currentSoundCueSegment;
        cueSegment.buffer += rawText;
        cueSegment.body.hidden = !cueSegment.buffer.trim();
        cueSegment.raw.hidden = !cueSegment.buffer.trim();
        if (options.scroll === false) flushPlainTextSegment(cueSegment);
        else schedulePlainTextSegment(cueSegment, function() { scheduleScrollBottom({ stream: true }); });
      } else {
        const segment = ensureThinkingTextSegment(node);
        segment.buffer += rawText;
        segment.node.hidden = !segment.buffer.trim();
        if (options.scroll === false) flushPlainTextSegment(segment);
        else schedulePlainTextSegment(segment, function() { scheduleScrollBottom({ stream: true }); });
      }
      node.wrap.hidden = !node.buffer.trim() && !(node.soundCueCount > 0);
      return node;
    }

    function appendThinkingSoundCue(thinking, cue, beforeNode = null, options = {}) {
      const node = thinking || addThinkingCard('', beforeNode);
      const block = createSoundCueEvent(cue, 'thinking', options);
      const body = soundCueEventBody(block);
      body.classList.remove('markdown');
      const raw = document.createElement('pre');
      raw.className = 'thinking-raw';
      raw.hidden = true;
      body.appendChild(raw);
      node.card.appendChild(block);
      node.soundCueCount = (node.soundCueCount || 0) + 1;
      const segment = { block, body, raw, buffer: '' };
      node.soundCueSegments.push(segment);
      node.currentSoundCueSegment = segment;
      node.currentSegment = null;
      node.wrap.hidden = false;
      scheduleScrollBottom({ stream: true });
      return { thinking: node, block };
    }

    function finishThinking(thinking) {
      if (!thinking) return;
      (thinking.segments || []).forEach(flushPlainTextSegment);
      (thinking.soundCueSegments || []).forEach(flushPlainTextSegment);
      if (!String(thinking.buffer || '').trim() && !(thinking.soundCueCount > 0)) {
        thinking.wrap.hidden = true;
        return;
      }
      thinking.meta.textContent = assistantMeta(t('thinkingComplete'));
    }

    function startMatrix(node) {
      stopMatrix();
      state.matrixTimer = setInterval(() => {
        frame = (frame + 1) % frames.length;
        if (node) node.textContent = frames[frame];
      }, 100);
    }
    function stopMatrix() {
      if (state.matrixTimer) clearInterval(state.matrixTimer);
      state.matrixTimer = null;
    }

    function summarizeToolValue(value, max = 760) {
      const text = String(value || '').replace(/\r\n/g, '\n').trim();
      if (!text) return t('noOutput');
      const lines = text.split('\n').filter(Boolean).slice(0, 10).join('\n');
      return lines.length > max ? `${lines.slice(0, max)}\n…[${t('summaryTruncated')}]` : lines;
    }

    function formatToolStatus(status) {
      const value = String(status || 'running').toLowerCase();
      if (value === 'done') return t('toolStatusDone');
      if (value === 'error') return t('toolStatusError');
      if (value === 'skipped') return t('toolStatusSkipped');
      if (value === 'called') return t('toolStatusCalled');
      if (value === 'running') return t('toolStatusRunning');
      return status;
    }

    function renderToolCard(entry, highlight = false) {
      const el = document.createElement('div');
      el.className = `tool-card${highlight ? ' highlight' : ''}`;
      const head = document.createElement('div');
      head.className = 'tool-head';
      const left = document.createElement('span');
      left.textContent = `⚡ ${entry.name || t('tool')}`;
      const right = document.createElement('span');
      right.textContent = formatToolStatus(entry.status);
      const result = document.createElement('div');
      result.className = 'tool-result';
      const detail = entry.detail ? `${entry.detail}\n` : '';
      result.textContent = `${detail}${summarizeToolValue(entry.result || t('waitingOutput'))}`;
      head.append(left, right);
      el.append(head, result);
      return el;
    }

    function createToolGroup(parent) {
      const group = document.createElement('div');
      group.className = 'tool-group';
      const toggle = document.createElement('button');
      toggle.className = 'tool-toggle';
      toggle.type = 'button';
      const latest = document.createElement('div');
      latest.className = 'tool-latest';
      const list = document.createElement('div');
      list.className = 'tool-list';
      group.append(toggle, latest, list);
      parent.appendChild(group);

      const model = { entries: [], expanded: false };
      function getHighlighted() {
        for (let i = model.entries.length - 1; i >= 0; i--) {
          if (model.entries[i].status === 'running') return model.entries[i];
        }
        return model.entries[model.entries.length - 1];
      }
      function render() {
        group.classList.toggle('has-tools', model.entries.length > 0);
        group.classList.toggle('expanded', model.expanded);
        toggle.textContent = (model.expanded ? '\u25be ' : '\u25b8 ') + t('toolsLabel') + ' (' + model.entries.length + ')';
        latest.innerHTML = '';
        list.innerHTML = '';
        const highlighted = getHighlighted();
        if (highlighted) latest.appendChild(renderToolCard(highlighted, true));
        for (const entry of model.entries) list.appendChild(renderToolCard(entry, entry === highlighted));
      }
      toggle.addEventListener('click', () => {
        model.expanded = !model.expanded;
        render();
      });
      render();
      return {
        upsert(tool) {
          let entry = model.entries.find(item => item.id && tool.id && item.id === tool.id);
          if (!entry) {
            entry = { id: tool.id || `tool-${model.entries.length + 1}`, name: tool.name || 'tool', detail: tool.detail || '', status: 'running', result: '' };
            model.entries.push(entry);
          }
          entry.name = tool.name || entry.name;
          entry.detail = tool.detail ?? entry.detail;
          entry.status = tool.status || entry.status || 'running';
          if (tool.result != null) entry.result = tool.result;
          render();
          scrollBottom();
          return entry;
        }
      };
    }

    async function api(url, options) {
      const res = await fetch(url, options);
      if (!res.ok) throw new Error(await res.text());
      return await res.json();
    }

    function showEasterEgg() {
      const overlay = $('easterEggOverlay');
      if (!overlay) return;
      overlay.classList.add('active');
      overlay.setAttribute('aria-hidden', 'false');
      easterEgg.clicks = 0;
      easterEgg.lastClickAt = 0;
    }

    function hideEasterEgg() {
      const overlay = $('easterEggOverlay');
      if (!overlay) return;
      overlay.classList.remove('active');
      overlay.setAttribute('aria-hidden', 'true');
    }

    function handleMatdanceTitlePress(event) {
      const now = performance.now();
      if (now - easterEgg.lastClickAt > easterEgg.windowMs) easterEgg.clicks = 0;
      easterEgg.clicks++;
      easterEgg.lastClickAt = now;
      if (easterEgg.clicks >= 5) showEasterEgg();
      if (event) event.stopPropagation();
    }

    async function switchTab(tab) {
      const target = ['home', 'chat', 'agent', 'schedule', 'skills', 'settings', 'lab', 'memory'].includes(tab) ? tab : 'home';
      state.activeTab = target;
      $('homePage').classList.toggle('active', target === 'home');
      $('chatTab').classList.toggle('active', target === 'chat');
      $('agentTab').classList.toggle('active', target === 'agent');
      $('scheduleTab').classList.toggle('active', target === 'schedule');
      $('skillsTab').classList.toggle('active', target === 'skills');
      $('labTab').classList.toggle('active', target === 'lab');
      $('settingsTab').classList.toggle('active', target === 'settings');
      $('memoryTab').classList.toggle('active', target === 'memory');
      document.querySelectorAll('.planet-chip[data-tab]').forEach(button => {
        button.classList.toggle('active', button.dataset.tab === target);
      });
      if (target === 'home') {
        initStarMap();
        startStarMap();
        resizeStarMap();
        requestAnimationFrame(resizeStarMap);
        setTimeout(resizeStarMap, 120);
        starScene.hover = null;
        starScene.canvas?.classList.remove('hot-target');
        starScene.lastInput = performance.now() - 3000;
        updatePlanetHud(null);
      } else {
        const panel = target === 'chat' ? $('chatTab') : target === 'agent' ? $('agentTab') : target === 'schedule' ? $('scheduleTab') : target === 'skills' ? $('skillsTab') : target === 'lab' ? $('labTab') : target === 'memory' ? $('memoryTab') : $('settingsTab');
        panel.classList.remove('page-enter');
        void panel.offsetWidth;
        panel.classList.add('page-enter');
        setTimeout(() => panel.classList.remove('page-enter'), 700);
      }
      if (target === 'agent') await loadAgentConfig();
      if (target === 'schedule') await loadScheduledTasks();
      if (target === 'skills') await loadSkills();
      if (target === 'lab') await loadLab();
      if (target === 'settings') { await Promise.all([loadMultiModalConfig(), loadSecuritySettings(), loadSkillValidationSettings()]); updateSettingsSection(); }
      else { stopRuntimeEventsSync(); }
      if (target === 'memory') await loadMemory();
      if (target === 'chat') {
        await loadMultiModalConfig();
        if (state.session && !state.busy) await loadSession();
        startChatPolling();
        updateChatJumpButton();
      }
      else { stopChatPolling(); }
    }

    async function goHome() {
      hideCommandMenu();
      await switchTab('home');
    }

    function setAgentConfigState(text) {
      const node = $('agentConfigState');
      if (node) {
        node.textContent = text;
        node.dataset.dynamic = text ? '1' : '';
      }
    }

    function clearAgentConfigView() {
      ['configName','configBaseUrl','configModelId','configContextWindow','configMaxOutputToken','configApiKey','configTemperature'].forEach(function(id) {
        const node = $(id);
        if (node) node.value = '';
      });
      if ($('configMaxConcurrency')) $('configMaxConcurrency').value = '1';
      ['configApiType','agentConfigSelect'].forEach(function(id) {
        const node = $(id);
        if (node) node.innerHTML = '';
      });
      ['agentSessionCount','agentKeyState','agentHotMemoryState','agentCoreMemoryState','agentConfigPath','agentWorkspacePath','agentMemoryPath','agentIconsPath'].forEach(function(id) {
        const node = $(id);
        if (node) node.textContent = '-';
      });
      const preview = $('agentAvatarPreview');
      if (preview) preview.innerHTML = '';
      if ($('agentAvatarName')) $('agentAvatarName').textContent = '-';
      if ($('agentAvatarHint')) $('agentAvatarHint').textContent = '';
      setAgentConfigState(t('noAgents'));
    }

    function syncAgentSelectors() {
      if ($('agentSelect')) $('agentSelect').value = state.agent || '';
      if ($('agentConfigSelect')) $('agentConfigSelect').value = state.agent || '';
      if ($('scheduleAgentSelect')) $('scheduleAgentSelect').value = state.agent || '';
      if ($('skillsAgentSelect')) $('skillsAgentSelect').value = state.agent || '';
      if ($('labAgentSelect')) $('labAgentSelect').value = state.agent || '';
      if ($('memoryAgentSelect')) $('memoryAgentSelect').value = state.agent || '';
      if ($('runtimeEventsAgentSelect')) $('runtimeEventsAgentSelect').value = state.runtimeEventsAgent || state.agent || '';
    }

    function renderAgentAvatarPreview(data) {
      const preview = $('agentAvatarPreview');
      if (!preview) return;
      const config = data?.config ?? {};
      const name = config.displayName ?? config.name ?? data?.agent ?? agentDisplayName();
      preview.innerHTML = '';
      const nameNode = $('agentAvatarName');
      if (nameNode) nameNode.textContent = name;
      const hintNode = $('agentAvatarHint');
      if (hintNode) hintNode.textContent = t('avatarHint').replace('{agent}', data?.agent ?? state.agent ?? 'agent');
      if (config.iconUrl) {
        const img = document.createElement('img');
        img.src = config.iconUrl;
        img.alt = name;
        preview.appendChild(img);
        return;
      }
      preview.textContent = config.initial ?? agentInitial(name);
    }

    function fillAgentConfig(data) {
      if (!data || !data.config) return;
      renderAgentAvatarPreview(data);
      const config = data.config;
      state.modelProviders = Array.isArray(data.providers) ? data.providers : state.modelProviders;
      $('configName').value = data.agent || config.name || '';
      $('configBaseUrl').value = config.baseUrl || '';
      $('configModelId').value = config.modelId || '';
      $('configContextWindow').value = config.contextWindow || '';
      $('configMaxOutputToken').value = config.maxOutputToken || '';
      $('configMaxConcurrency').value = config.maxConcurrency || 1;
      $('configTemperature').value = config.temperature ?? '';
      $('configApiKey').value = '';

      const apiTypeSelect = $('configApiType');
      apiTypeSelect.innerHTML = '';
      const apiTypes = data.apiTypes && data.apiTypes.length ? data.apiTypes : [config.apiType || 'openai_chat'];
      if (config.apiType && !apiTypes.includes(config.apiType)) apiTypes.push(config.apiType);
      for (const apiType of apiTypes) {
        const opt = document.createElement('option');
        opt.value = apiType;
        opt.textContent = apiTypeLabel(apiType);
        apiTypeSelect.appendChild(opt);
      }
      apiTypeSelect.value = apiTypes.includes(config.apiType) ? config.apiType : apiTypes[0];
      fillModelOptions(apiTypeSelect.value);
      syncProviderDefaults(false);

      $('agentSessionCount').textContent = config.sessionCount ?? 0;
      $('agentKeyState').textContent = config.hasApiKey ? t('configured') : t('missing');
      $('agentHotMemoryState').textContent = config.hotMemoryExists ? t('ready') : t('empty');
      $('agentCoreMemoryState').textContent = config.coreMemoryExists ? t('ready') : t('empty');
      $('agentConfigPath').textContent = config.configPath || '-';
      $('agentWorkspacePath').textContent = config.workspacePath || '-';
      $('agentMemoryPath').textContent = config.memoryPath || '-';
      $('agentIconsPath').textContent = config.iconsPath ? config.iconsPath : '-';
      setAgentConfigState(t('loaded') + ' ' + data.agent);
    }

    function apiTypeLabel(apiType) {
      const provider = providerById(apiType);
      if (provider) return provider.label || provider.id || apiType;
      if (apiType === 'openai_chat') return state.lang === 'zh' ? 'openai_chat - OpenAI 兼容，支持工具' : 'openai_chat - OpenAI-compatible, tools supported';
      if (apiType === 'anthropic') return state.lang === 'zh' ? 'anthropic - Messages 兼容，支持工具' : 'anthropic - Messages-compatible, tools supported';
      return apiType;
    }

    function providerById(id) {
      return (state.modelProviders || []).find(function(provider) { return provider.id === id; }) || null;
    }

    function modelPreset(providerId, modelId) {
      const provider = providerById(providerId);
      const models = provider?.models || [];
      return models.find(function(model) { return model.id === modelId; }) || null;
    }

    function providerApiKeyUrl(provider) {
      return provider?.apiKeyUrl || provider?.api_key_url || '';
    }

    function updateProviderApiKeyLink(provider) {
      const link = $('configApiKeyLink');
      if (!link) return;
      const url = providerApiKeyUrl(provider);
      link.textContent = t('apiKeyLink');
      if (!url) {
        link.hidden = true;
        link.removeAttribute('href');
        return;
      }
      link.href = url;
      link.hidden = false;
    }

    function formatTokenCount(value) {
      const n = Number(value || 0);
      if (!Number.isFinite(n) || n <= 0) return '';
      if (n >= 1_000_000) return (n / 1_000_000).toFixed(n % 1_000_000 === 0 ? 0 : 1) + 'M';
      if (n >= 1_000) return (n / 1_000).toFixed(n % 1_000 === 0 ? 0 : 1) + 'K';
      return String(n);
    }

    function modelMeta(model) {
      const meta = [];
      if (model.supportsThinking) meta.push('thinking');
      if (model.contextWindow) meta.push(formatTokenCount(model.contextWindow) + ' ctx');
      if (model.maxInputToken) meta.push(formatTokenCount(model.maxInputToken) + ' in');
      if (model.maxOutputToken) meta.push(formatTokenCount(model.maxOutputToken) + ' out');
      return meta.join(' | ');
      return meta.join(' · ');
    }

    function modelOptionItems() {
      return Array.from($('configModelOptions')?.querySelectorAll('.model-combo-option') || []);
    }

    function setModelMenuOpen(open) {
      const combo = $('configModelCombo');
      const input = $('configModelId');
      const menu = $('configModelOptions');
      if (!combo || !input || !menu) return;
      const hasItems = modelOptionItems().length > 0;
      const visible = !!open && (hasItems || menu.querySelector('.model-combo-empty'));
      menu.hidden = !visible;
      combo.classList.toggle('open', visible);
      input.setAttribute('aria-expanded', visible ? 'true' : 'false');
      if (!visible) state.modelMenuIndex = -1;
    }

    function setModelMenuIndex(index) {
      const items = modelOptionItems();
      if (!items.length) return;
      const next = ((index % items.length) + items.length) % items.length;
      state.modelMenuIndex = next;
      items.forEach(function(item, i) {
        item.classList.toggle('active', i === next);
        item.setAttribute('aria-selected', i === next ? 'true' : 'false');
      });
      items[next].scrollIntoView({ block: 'nearest' });
    }

    function fillModelOptions(providerId, filterText) {
      const menu = $('configModelOptions');
      if (!menu) return;
      menu.innerHTML = '';
      const provider = providerById(providerId);
      const query = String(filterText || '').trim().toLowerCase();
      const models = (provider?.models || []).filter(function(model) {
        if (!query) return true;
        return String(model.id || '').toLowerCase().includes(query) || String(model.notes || '').toLowerCase().includes(query);
      });
      for (const model of models) {
        const option = document.createElement('button');
        option.type = 'button';
        option.className = 'model-combo-option';
        option.dataset.modelId = model.id;
        option.setAttribute('role', 'option');
        option.setAttribute('aria-selected', model.id === $('configModelId')?.value ? 'true' : 'false');
        const id = document.createElement('span');
        id.className = 'model-combo-id';
        id.textContent = model.id;
        const meta = document.createElement('span');
        meta.className = 'model-combo-meta';
        meta.textContent = modelMeta(model);
        option.appendChild(id);
        option.appendChild(meta);
        option.addEventListener('click', function() { selectModelPreset(model.id); });
        menu.appendChild(option);
      }
      if (!models.length && provider?.models?.length) {
        const empty = document.createElement('div');
        empty.className = 'model-combo-empty';
        empty.textContent = t('modelDropdownEmpty');
        menu.appendChild(empty);
      }
      state.modelMenuIndex = -1;
    }

    function selectModelPreset(modelId) {
      $('configModelId').value = modelId;
      syncProviderDefaults(false);
      applyPresetTokenLimits(modelPreset($('configApiType').value, modelId), true);
      applyPresetTemperature(modelPreset($('configApiType').value, modelId), true);
      setModelMenuOpen(false);
      $('configModelId').focus();
    }

    function toggleModelMenu(forceOpen) {
      const providerId = $('configApiType').value;
      fillModelOptions(providerId);
      const menu = $('configModelOptions');
      const open = forceOpen ?? !!menu?.hidden;
      setModelMenuOpen(open);
      if (open) setModelMenuIndex(0);
    }

    function handleModelComboKeydown(event) {
      const menu = $('configModelOptions');
      const items = modelOptionItems();
      if (event.key === 'ArrowDown') {
        event.preventDefault();
        if (menu?.hidden) toggleModelMenu(true);
        else setModelMenuIndex((state.modelMenuIndex ?? -1) + 1);
      } else if (event.key === 'ArrowUp') {
        event.preventDefault();
        if (menu?.hidden) toggleModelMenu(true);
        else setModelMenuIndex((state.modelMenuIndex ?? items.length) - 1);
      } else if (event.key === 'Enter' && !menu?.hidden && items.length && (state.modelMenuIndex ?? -1) >= 0) {
        event.preventDefault();
        selectModelPreset(items[state.modelMenuIndex].dataset.modelId);
      } else if (event.key === 'Escape') {
        setModelMenuOpen(false);
      }
    }

    function syncProviderDefaults(overwriteModel) {
      const providerId = $('configApiType').value;
      const provider = providerById(providerId);
      fillModelOptions(providerId);
      updateProviderApiKeyLink(provider);
      if (!provider) {
        $('configBaseUrl').readOnly = false;
        $('configContextWindow').readOnly = false;
        $('configMaxOutputToken').readOnly = false;
        return;
      }
      const managed = !!provider.managedDefaults;
      const locksBaseUrl = !!provider.locksBaseUrl;
      const locksTokenLimits = !!provider.locksTokenLimits;
      $('configBaseUrl').readOnly = locksBaseUrl;
      $('configContextWindow').readOnly = locksTokenLimits;
      $('configMaxOutputToken').readOnly = locksTokenLimits;
      if (locksBaseUrl || (managed && overwriteModel)) $('configBaseUrl').value = provider.baseUrl || $('configBaseUrl').value;
      const models = provider.models || [];
      if (overwriteModel && models.length) $('configModelId').value = models[0].id;
      const preset = modelPreset(providerId, $('configModelId').value);
      const defaultPreset = models[0] || null;
      if (locksTokenLimits) applyPresetTokenLimits(preset || defaultPreset, true);
      else if (overwriteModel) applyPresetTokenLimits(preset || defaultPreset, true);
      applyPresetTemperature(preset, overwriteModel);
    }

    function applyPresetTokenLimits(preset, force) {
      if (!preset) return;
      if (force || $('configContextWindow').value === '') {
        $('configContextWindow').value = preset.contextWindow || '';
      }
      if (force || $('configMaxOutputToken').value === '') {
        $('configMaxOutputToken').value = preset.maxOutputToken || '';
      }
    }

    function applyPresetTemperature(preset, force) {
      if (!preset || preset.temperature == null) return;
      if (force || $('configTemperature').value === '') {
        $('configTemperature').value = preset.temperature;
      }
    }

    async function loadAgentConfig() {
      if (!state.agent || !$('agentConfigSelect')) return;
      setAgentConfigState(t('loadingConfig'));
      const data = await api(`/api/agent-config?agent=${encodeURIComponent(state.agent)}`);
      fillAgentConfig(data);
    }

    async function saveAgentConfig() {
      if (!state.agent) return;
      setAgentConfigState(t('savingConfig'));
      const payload = {
        agent: state.agent,
        baseUrl: $('configBaseUrl').value.trim(),
        modelId: $('configModelId').value.trim(),
        apiType: $('configApiType').value,
        apiKey: $('configApiKey').value.trim(),
        contextWindow: Number($('configContextWindow').value),
        maxOutputToken: Number($('configMaxOutputToken').value),
        maxConcurrency: Number($('configMaxConcurrency').value || 1),
        temperature: Number($('configTemperature').value)
      };
      const data = await api('/api/agent-config', {
        method: 'POST',
        headers: { 'content-type': 'application/json' },
        body: JSON.stringify(payload)
      });
      fillAgentConfig(data);
      await loadAgents(false);
      setAgentConfigState(t('saved') + ' ' + data.agent);
    }

    async function loadMultiModalConfig() {
      if (!state.agent) return null;
      try {
        const data = await api(`/api/multimodal-config?agent=${encodeURIComponent(state.agent)}`);
        state.multimodal = data;
        renderMultiModalSettings();
        updateVoiceUi();
        updateLabStatus();
        return data;
      } catch (err) {
        state.multimodal = null;
        updateVoiceUi();
        const status = $('multiStatus');
        if (status) status.textContent = err.message;
        return null;
      }
    }

    function boolOptionValue(value) {
      if (value === true) return 'true';
      if (value === false) return 'false';
      return '';
    }

    function optionSelected(actual, value) {
      return String(actual ?? '') === String(value) ? ' selected' : '';
    }

    function profileHasKey(component) {
      return !!(component && (component.hasApiKey || component.has_api_key));
    }

    function isAliyunQwenTtsMode(value) {
      return String(value ?? '').toLowerCase() === 'aliyun_qwen_tts';
    }

    function escapeAttr(value) {
      const map = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' };
      return String(value ?? '').replace(/[&<>"']/g, char => map[char]);
    }

    function renderBoolSelect(id, label, value) {
      const actual = boolOptionValue(value);
      return `<div class="field"><label for="${id}"><span>${escapeHtml(label)}</span><span>${escapeHtml(t('multiMetaBool'))}</span></label><select id="${id}"><option value=""${optionSelected(actual, '')}>${escapeHtml(t('multiOptionInherit'))}</option><option value="true"${optionSelected(actual, 'true')}>${escapeHtml(t('multiOptionEnabled'))}</option><option value="false"${optionSelected(actual, 'false')}>${escapeHtml(t('multiOptionDisabled'))}</option></select></div>`;
    }

    function renderTextField(id, label, value, type = 'text', placeholder = '') {
      const wide = /(BaseUrl|ApiKey)$/i.test(id) ? ' multimodal-wide-field' : '';
      const meta = type === 'password' ? t('multiMetaWriteOnly') : t('multiMetaValue');
      return `<div class="field${wide}"><label for="${id}"><span>${escapeHtml(label)}</span><span>${escapeHtml(meta)}</span></label><input id="${id}" type="${type}" value="${escapeAttr(value ?? '')}" placeholder="${escapeAttr(placeholder)}" autocomplete="new-password" /></div>`;
    }

    function renderSelectField(id, label, value, options) {
      const actual = String(value ?? '');
      const choices = options.slice();
      if (actual && !choices.some(item => String(item.value) === actual)) {
        choices.push({ value: actual, label: actual });
      }
      const body = choices.map(item => `<option value="${escapeAttr(item.value)}"${optionSelected(actual, item.value)}>${escapeHtml(item.label)}</option>`).join('');
      return `<div class="field"><label for="${id}"><span>${escapeHtml(label)}</span><span>${escapeHtml(t('multiMetaMode'))}</span></label><select id="${id}">${body}</select></div>`;
    }

    function imageProfilesForProfile(profile) {
      const models = profile?.imageModels || profile?.image_models;
      if (Array.isArray(models) && models.length) return models;
      return [profile?.image ?? {}];
    }

    function ttsProfilesForProfile(profile) {
      const models = profile?.ttsModels || profile?.tts_models;
      if (Array.isArray(models) && models.length) return models;
      return [profile?.tts ?? {}];
    }

    function searchProfilesForProfile(profile) {
      const models = profile?.searchModels || profile?.search_models;
      if (Array.isArray(models) && models.length) return models;
      return [profile?.search ?? {}];
    }

    function renderImageProfile(prefix, image, index) {
      const cardPrefix = prefix + 'ImageModel' + index;
      return `
        <section class="image-profile-card" data-image-profile="${index}">
          <div class="image-profile-head">
            <strong>${escapeHtml(image?.name || image?.id || ('Image model ' + (index + 1)))}</strong>
            <button class="ghost image-profile-remove" type="button" data-remove-image-profile="${index}">${escapeHtml(t('multiRemove'))}</button>
          </div>
          <div class="multimodal-form-grid">
            ${renderBoolSelect(cardPrefix + 'Enabled', t('multiFieldEnabled'), image?.enabled)}
            ${renderTextField(cardPrefix + 'Id', t('multiFieldProfileId'), image?.id, 'text', 'flux-fast')}
            ${renderTextField(cardPrefix + 'Name', t('multiFieldDisplayName'), image?.name, 'text', 'Flux fast')}
            ${renderSelectField(cardPrefix + 'EndpointMode', t('multiFieldEndpoint'), image?.endpointMode ?? image?.endpoint_mode ?? '', endpointModeOptions())}
            ${renderTextField(cardPrefix + 'BaseUrl', t('multiFieldBaseUrl'), image?.baseUrl ?? image?.base_url)}
            ${renderTextField(cardPrefix + 'ApiKey', t('multiFieldApiKey'), '', 'password', profileHasKey(image) ? t('multiApiKeyConfigured') : t('multiApiKeyEmpty'))}
            ${renderTextField(cardPrefix + 'Model', t('multiFieldModel'), image?.model)}
            ${renderTextField(cardPrefix + 'Size', t('multiFieldSize'), image?.size, 'text', '1024x1024')}
            ${renderTextField(cardPrefix + 'Quality', t('multiFieldQuality'), image?.quality, 'text', 'auto')}
            ${renderTextField(cardPrefix + 'Format', t('multiFieldFormat'), image?.outputFormat ?? image?.output_format, 'text', 'png')}
          </div>
        </section>`;
    }

    function renderTtsProfile(prefix, tts, index) {
      const cardPrefix = prefix + 'TtsModel' + index;
      const ttsEndpointMode = tts?.endpointMode ?? tts?.endpoint_mode ?? '';
      const ttsUsesAliyun = isAliyunQwenTtsMode(ttsEndpointMode);
      return `
        <section class="image-profile-card tts-profile-card" data-tts-profile="${index}">
          <div class="image-profile-head">
            <strong>${escapeHtml(tts?.name || tts?.id || ('TTS model ' + (index + 1)))}</strong>
            <button class="ghost image-profile-remove tts-profile-remove" type="button" data-remove-tts-profile="${index}">${escapeHtml(t('multiRemove'))}</button>
          </div>
          <div class="multimodal-form-grid">
            ${renderSelectField(cardPrefix + 'Mode', t('multiFieldMode'), tts?.mode ?? '', [
              { value: '', label: t('multiOptionInherit') },
              { value: 'off', label: t('multiOptionOff') },
              { value: 'chat_visible_only', label: t('multiOptionChatVisible') },
              { value: 'always', label: t('multiOptionAlways') }
            ])}
            ${renderTextField(cardPrefix + 'Id', t('multiFieldProfileId'), tts?.id, 'text', 'narration')}
            ${renderTextField(cardPrefix + 'Name', t('multiFieldDisplayName'), tts?.name, 'text', 'Narration voice')}
            ${renderSelectField(cardPrefix + 'EndpointMode', t('multiFieldEndpoint'), ttsEndpointMode, ttsEndpointModeOptions())}
            ${renderBoolSelect(cardPrefix + 'AutoPlay', t('multiFieldAutoPlay'), tts?.autoPlay)}
            ${ttsUsesAliyun ? '' : renderTextField(cardPrefix + 'BaseUrl', t('multiFieldBaseUrl'), tts?.baseUrl ?? tts?.base_url)}
            ${renderTextField(cardPrefix + 'ApiKey', t('multiFieldApiKey'), '', 'password', profileHasKey(tts) ? t('multiApiKeyConfigured') : t('multiApiKeyEmpty'))}
            ${renderTextField(cardPrefix + 'Model', t('multiFieldModel'), tts?.model)}
            ${renderTextField(cardPrefix + 'Voice', t('multiFieldVoice'), tts?.voice, 'text', 'alloy')}
            ${renderTextField(cardPrefix + 'LanguageType', t('multiFieldLanguage'), tts?.languageType ?? tts?.language_type, 'text', 'Chinese')}
            ${renderTextField(cardPrefix + 'Instructions', t('multiFieldInstructions'), tts?.instructions, 'text', 'optional')}
            ${renderBoolSelect(cardPrefix + 'OptimizeInstructions', t('multiFieldOptimizeInstructions'), tts?.optimizeInstructions ?? tts?.optimize_instructions)}
            ${renderTextField(cardPrefix + 'Format', t('multiFieldFormat'), tts?.format, 'text', 'mp3')}
          </div>
          ${ttsUsesAliyun ? `<p class="multimodal-note">${escapeHtml(t('multiAliyunBaseNote'))}</p>` : ''}
        </section>`;
    }

    function searchProviderOptions() {
      return [
        { value: 'tavily', label: t('multiOptionSearchTavily') },
        { value: 'brave', label: t('multiOptionSearchBrave') },
        { value: 'firecrawl', label: t('multiOptionSearchFirecrawl') },
        { value: 'custom', label: t('multiOptionSearchCustom') }
      ];
    }

    function renderSearchProfile(prefix, search, index) {
      const cardPrefix = prefix + 'SearchModel' + index;
      return `
        <section class="image-profile-card search-profile-card" data-search-profile="${index}">
          <div class="image-profile-head">
            <strong>${escapeHtml(search?.name || search?.id || ('Search provider ' + (index + 1)))}</strong>
            <button class="ghost image-profile-remove search-profile-remove" type="button" data-remove-search-profile="${index}">${escapeHtml(t('multiRemove'))}</button>
          </div>
          <div class="multimodal-form-grid">
            ${renderBoolSelect(cardPrefix + 'Enabled', t('multiFieldEnabled'), search?.enabled)}
            ${renderTextField(cardPrefix + 'Id', t('multiFieldProfileId'), search?.id, 'text', 'tavily')}
            ${renderTextField(cardPrefix + 'Name', t('multiFieldDisplayName'), search?.name, 'text', 'Tavily')}
            ${renderSelectField(cardPrefix + 'Provider', t('multiFieldProvider'), search?.provider ?? 'tavily', searchProviderOptions())}
            ${renderTextField(cardPrefix + 'BaseUrl', t('multiFieldBaseUrl'), search?.baseUrl ?? search?.base_url)}
            ${renderTextField(cardPrefix + 'EndpointPath', t('multiFieldEndpointPath'), search?.endpointPath ?? search?.endpoint_path, 'text', 'search')}
            ${renderTextField(cardPrefix + 'ApiKey', t('multiFieldApiKey'), '', 'password', profileHasKey(search) ? t('multiApiKeyConfigured') : t('multiApiKeyEmpty'))}
            ${renderTextField(cardPrefix + 'MaxResults', t('multiFieldMaxResults'), search?.maxResults ?? search?.max_results, 'number', '5')}
          </div>
        </section>`;
    }

    function renderMultiProfile(prefix, profile) {
      const stt = profile?.stt ?? {};
      const imageProfiles = imageProfilesForProfile(profile);
      const ttsProfiles = ttsProfilesForProfile(profile);
      const searchProfiles = searchProfilesForProfile(profile);
      const node = $(prefix + 'Form');
      if (!node) return;
      node.innerHTML = `
        <section class="multimodal-section">
          <div class="multimodal-section-title"><span>${escapeHtml(t('multiImageProfiles'))}</span><button class="ghost image-profile-add" type="button" data-add-image-profile="${prefix}">${escapeHtml(t('multiAdd'))}</button></div>
          <div id="${prefix}ImageProfiles" class="image-profile-list">
            ${imageProfiles.map((image, index) => renderImageProfile(prefix, image, index)).join('')}
          </div>
        </section>
        <section class="multimodal-section">
          <div class="multimodal-section-title"><span>${escapeHtml(t('multiTtsProfiles'))}</span><button class="ghost tts-profile-add" type="button" data-add-tts-profile="${prefix}">${escapeHtml(t('multiAdd'))}</button></div>
          <div id="${prefix}TtsProfiles" class="image-profile-list tts-profile-list">
            ${ttsProfiles.map((tts, index) => renderTtsProfile(prefix, tts, index)).join('')}
          </div>
        </section>
        <section class="multimodal-section">
          <div class="multimodal-section-title"><span>${escapeHtml(t('multiSearchProfiles'))}</span><button class="ghost search-profile-add" type="button" data-add-search-profile="${prefix}">${escapeHtml(t('multiAdd'))}</button></div>
          <div id="${prefix}SearchProfiles" class="image-profile-list search-profile-list">
            ${searchProfiles.map((search, index) => renderSearchProfile(prefix, search, index)).join('')}
          </div>
        </section>
        <section class="multimodal-section">
          <div class="multimodal-section-title"><span>${escapeHtml(t('multiSpeechToText'))}</span></div>
          <div class="multimodal-form-grid">
            ${renderBoolSelect(prefix + 'SttEnabled', t('multiFieldEnabled'), stt.enabled)}
            ${renderBoolSelect(prefix + 'SttSendAfter', t('multiFieldSendAfter'), stt.sendAfterTranscription)}
          </div>
          <p class="multimodal-note">${escapeHtml(t('multiSttBrowserNote'))}</p>
        </section>`;
    }

    function renderMultiModalSettings() {
      if (!state.multimodal) return;
      renderMultiProfile('multiGlobal', state.multimodal.global);
      const status = $('multiStatus');
      if (status) {
        const effective = state.multimodal.effective ?? {};
        const image = effective.image ?? {};
        const tts = effective.tts ?? {};
        const stt = effective.stt ?? {};
        const search = effective.search ?? {};
        status.textContent = `image ${image.enabled ? 'on' : 'off'}:${image.endpointMode || 'native'} / tts ${tts.mode || 'off'}:${tts.endpointMode || 'native'} / search ${search.enabled ? 'on' : 'off'}:${search.provider || 'none'} / stt ${stt.enabled ? 'on' : 'off'}:${stt.endpointMode || 'native'}`;
      }
    }

    function endpointModeOptions() {
      return [
        { value: '', label: t('multiOptionInherit') },
        { value: 'native', label: t('multiOptionNativeImage') }
      ];
    }

    function ttsEndpointModeOptions() {
      return [
        { value: '', label: t('multiOptionInherit') },
        { value: 'native', label: t('multiOptionNativeSpeech') },
        { value: 'tts', label: t('multiOptionTts') },
        { value: 'aliyun_qwen_tts', label: t('multiOptionAliyunQwenTts') },
        { value: 'chat_completions', label: t('multiOptionChatCompletions') }
      ];
    }

    function readNullableBool(id) {
      const value = $(id)?.value ?? '';
      if (value === '') return null;
      return value === 'true';
    }

    function cleanInput(id) {
      const value = $(id)?.value ?? '';
      return value.trim() || null;
    }

    function readImageProfiles(prefix) {
      const cards = Array.from(document.querySelectorAll(`#${prefix}ImageProfiles [data-image-profile]`));
      const profiles = cards.map(function(card, order) {
        const index = card.dataset.imageProfile;
        const cardPrefix = prefix + 'ImageModel' + index;
        return {
          id: cleanInput(cardPrefix + 'Id') || ('image-' + (order + 1)),
          name: cleanInput(cardPrefix + 'Name') || cleanInput(cardPrefix + 'Id') || ('Image model ' + (order + 1)),
          enabled: readNullableBool(cardPrefix + 'Enabled'),
          endpoint_mode: cleanInput(cardPrefix + 'EndpointMode'),
          base_url: cleanInput(cardPrefix + 'BaseUrl'),
          api_key: cleanInput(cardPrefix + 'ApiKey'),
          model: cleanInput(cardPrefix + 'Model'),
          size: cleanInput(cardPrefix + 'Size'),
          quality: cleanInput(cardPrefix + 'Quality'),
          output_format: cleanInput(cardPrefix + 'Format')
        };
      });
      return profiles.length ? profiles : [{ id: 'default', name: 'Default image model', enabled: false }];
    }

    function readTtsProfiles(prefix) {
      const cards = Array.from(document.querySelectorAll(`#${prefix}TtsProfiles [data-tts-profile]`));
      const profiles = cards.map(function(card, order) {
        const index = card.dataset.ttsProfile;
        const cardPrefix = prefix + 'TtsModel' + index;
        const endpointMode = cleanInput(cardPrefix + 'EndpointMode');
        return {
          id: cleanInput(cardPrefix + 'Id') || ('tts-' + (order + 1)),
          name: cleanInput(cardPrefix + 'Name') || cleanInput(cardPrefix + 'Id') || ('TTS model ' + (order + 1)),
          mode: cleanInput(cardPrefix + 'Mode'),
          endpoint_mode: endpointMode,
          auto_play: readNullableBool(cardPrefix + 'AutoPlay'),
          base_url: isAliyunQwenTtsMode(endpointMode) ? null : cleanInput(cardPrefix + 'BaseUrl'),
          api_key: cleanInput(cardPrefix + 'ApiKey'),
          model: cleanInput(cardPrefix + 'Model'),
          voice: cleanInput(cardPrefix + 'Voice'),
          language_type: cleanInput(cardPrefix + 'LanguageType'),
          instructions: cleanInput(cardPrefix + 'Instructions'),
          optimize_instructions: readNullableBool(cardPrefix + 'OptimizeInstructions'),
          format: cleanInput(cardPrefix + 'Format')
        };
      });
      return profiles.length ? profiles : [{ id: 'default', name: 'Default TTS model', mode: 'off' }];
    }

    function readSearchProfiles(prefix) {
      const cards = Array.from(document.querySelectorAll(`#${prefix}SearchProfiles [data-search-profile]`));
      const profiles = cards.map(function(card, order) {
        const index = card.dataset.searchProfile;
        const cardPrefix = prefix + 'SearchModel' + index;
        const rawMax = cleanInput(cardPrefix + 'MaxResults');
        const maxResults = rawMax ? Math.max(1, Math.min(20, Number(rawMax) || 5)) : null;
        return {
          id: cleanInput(cardPrefix + 'Id') || ('search-' + (order + 1)),
          name: cleanInput(cardPrefix + 'Name') || cleanInput(cardPrefix + 'Id') || ('Search provider ' + (order + 1)),
          enabled: readNullableBool(cardPrefix + 'Enabled'),
          provider: cleanInput(cardPrefix + 'Provider') || 'custom',
          base_url: cleanInput(cardPrefix + 'BaseUrl'),
          endpoint_path: cleanInput(cardPrefix + 'EndpointPath'),
          api_key: cleanInput(cardPrefix + 'ApiKey'),
          max_results: maxResults
        };
      });
      return profiles.length ? profiles : [
        { id: 'tavily', name: 'Tavily', enabled: false, provider: 'tavily', base_url: 'https://api.tavily.com', endpoint_path: 'search', max_results: 5 },
        { id: 'brave', name: 'Brave Search', enabled: false, provider: 'brave', base_url: 'https://api.search.brave.com', endpoint_path: 'res/v1/web/search', max_results: 5 },
        { id: 'firecrawl', name: 'Firecrawl', enabled: false, provider: 'firecrawl', base_url: 'https://api.firecrawl.dev', endpoint_path: 'v1/search', max_results: 5 }
      ];
    }

    function readMultiProfile(prefix) {
      const imageProfiles = readImageProfiles(prefix);
      const ttsProfiles = readTtsProfiles(prefix);
      const searchProfiles = readSearchProfiles(prefix);
      return {
        image: imageProfiles[0],
        image_models: imageProfiles,
        tts: ttsProfiles[0],
        tts_models: ttsProfiles,
        search: searchProfiles[0],
        search_models: searchProfiles,
        stt: {
          enabled: readNullableBool(prefix + 'SttEnabled'),
          send_after_transcription: readNullableBool(prefix + 'SttSendAfter')
        }
      };
    }

    async function saveMultiModalConfig() {
      if (!state.agent) return;
      const status = $('multiStatus');
      if (status) status.textContent = 'Saving...';
      const data = await api('/api/multimodal-config', {
        method: 'POST',
        headers: { 'content-type': 'application/json' },
        body: JSON.stringify({
          agent: state.agent,
          global: readMultiProfile('multiGlobal')
        })
      });
      state.multimodal = data;
      renderMultiModalSettings();
      updateVoiceUi();
      updateLabStatus();
      if (status) status.textContent = 'Saved. ' + (data.configPath || '');
    }

    function mutateImageProfiles(mutator) {
      if (!state.multimodal) return;
      state.multimodal.global = readMultiProfile('multiGlobal');
      const models = state.multimodal.global.image_models || [];
      mutator(models);
      state.multimodal.global.image_models = models.length ? models : [{ id: 'default', name: 'Default image model', enabled: false }];
      state.multimodal.global.image = state.multimodal.global.image_models[0];
      renderMultiModalSettings();
    }

    function addImageProfile() {
      mutateImageProfiles(function(models) {
        const next = models.length + 1;
        models.push({
          id: 'image-' + next,
          name: 'Image model ' + next,
          enabled: true,
          endpoint_mode: 'native',
          base_url: models[0]?.base_url || 'https://api.openai.com/v1',
          model: '',
          size: models[0]?.size || '1024x1024',
          quality: 'auto',
          output_format: 'png'
        });
      });
    }

    function removeImageProfile(index) {
      mutateImageProfiles(function(models) {
        if (models.length <= 1) return;
        models.splice(index, 1);
      });
    }

    function mutateTtsProfiles(mutator) {
      if (!state.multimodal) return;
      state.multimodal.global = readMultiProfile('multiGlobal');
      const models = state.multimodal.global.tts_models || [];
      mutator(models);
      state.multimodal.global.tts_models = models.length ? models : [{ id: 'default', name: 'Default TTS model', mode: 'off' }];
      state.multimodal.global.tts = state.multimodal.global.tts_models[0];
      renderMultiModalSettings();
    }

    function addTtsProfile() {
      mutateTtsProfiles(function(models) {
        const next = models.length + 1;
        models.push({
          id: 'tts-' + next,
          name: 'TTS model ' + next,
          mode: 'chat_visible_only',
          endpoint_mode: 'native',
          base_url: models[0]?.base_url || 'https://api.openai.com/v1',
          model: models[0]?.model || 'gpt-4o-mini-tts',
          voice: models[0]?.voice || 'alloy',
          language_type: models[0]?.language_type || 'Chinese',
          format: models[0]?.format || 'mp3'
        });
      });
    }

    function removeTtsProfile(index) {
      mutateTtsProfiles(function(models) {
        if (models.length <= 1) return;
        models.splice(index, 1);
      });
    }

    function mutateSearchProfiles(mutator) {
      if (!state.multimodal) return;
      state.multimodal.global = readMultiProfile('multiGlobal');
      const models = state.multimodal.global.search_models || [];
      mutator(models);
      state.multimodal.global.search_models = models.length ? models : [
        { id: 'tavily', name: 'Tavily', enabled: false, provider: 'tavily', base_url: 'https://api.tavily.com', endpoint_path: 'search', max_results: 5 },
        { id: 'brave', name: 'Brave Search', enabled: false, provider: 'brave', base_url: 'https://api.search.brave.com', endpoint_path: 'res/v1/web/search', max_results: 5 },
        { id: 'firecrawl', name: 'Firecrawl', enabled: false, provider: 'firecrawl', base_url: 'https://api.firecrawl.dev', endpoint_path: 'v1/search', max_results: 5 }
      ];
      state.multimodal.global.search = state.multimodal.global.search_models[0];
      renderMultiModalSettings();
    }

    function addSearchProfile() {
      mutateSearchProfiles(function(models) {
        const next = models.length + 1;
        models.push({
          id: 'search-' + next,
          name: 'Search provider ' + next,
          enabled: true,
          provider: 'custom',
          base_url: '',
          endpoint_path: 'search',
          max_results: 5
        });
      });
    }

    function removeSearchProfile(index) {
      mutateSearchProfiles(function(models) {
        if (models.length <= 1) return;
        models.splice(index, 1);
      });
    }

    async function loadLab() {
      syncAgentSelectors();
      await loadMultiModalConfig();
      updateLabStatus();
    }

    function updateLabStatus() {
      const effective = state.multimodal?.effective ?? {};
      const image = effective.image ?? {};
      const imageModels = effective.imageModels || effective.image_models || [];
      const tts = effective.tts ?? {};
      const ttsModels = effective.ttsModels || effective.tts_models || [];
      const stt = effective.stt ?? {};
      const imageProfile = $('labImageProfile');
      if (imageProfile) {
        const current = imageProfile.value;
        imageProfile.innerHTML = '<option value="">auto profile</option>';
        for (const model of imageModels) {
          const option = document.createElement('option');
          option.value = model.id || model.name || model.model || '';
          option.textContent = (model.name || model.id || model.model || 'image model') + (model.enabled ? '' : ' (off)');
          option.disabled = !model.enabled;
          imageProfile.appendChild(option);
        }
        if (current && Array.from(imageProfile.options).some(option => option.value === current)) imageProfile.value = current;
      }
      const ttsProfile = $('labTtsProfile');
      if (ttsProfile) {
        const current = ttsProfile.value;
        ttsProfile.innerHTML = '<option value="">auto profile</option>';
        for (const model of ttsModels) {
          const option = document.createElement('option');
          option.value = model.id || model.name || model.voice || model.model || '';
          option.textContent = (model.name || model.id || model.voice || model.model || 'TTS model') + (model.mode && model.mode !== 'off' ? '' : ' (off)');
          option.disabled = !model.mode || model.mode === 'off';
          ttsProfile.appendChild(option);
        }
        if (current && Array.from(ttsProfile.options).some(option => option.value === current)) ttsProfile.value = current;
      }
      if ($('labImageStatus')) $('labImageStatus').textContent = image.enabled && hasEffectiveKey(image) ? 'ready' : 'disabled/missing key';
      if ($('labTtsStatus')) $('labTtsStatus').textContent = tts.mode && tts.mode !== 'off' && hasEffectiveKey(tts) ? tts.mode : 'disabled/missing key';
      if ($('labSttStatus')) $('labSttStatus').textContent = canUseStt() ? t('labBrowserSpeechReady') : t('labBrowserSpeechUnavailable');
    }

    async function runLabImage() {
      const prompt = $('labImagePrompt')?.value.trim();
      if (!prompt) return;
      const result = $('labImageResult');
      result.textContent = 'Generating...';
      try {
        const selectedProfile = $('labImageProfile')?.value || null;
        const data = await api('/api/image-generation', {
          method: 'POST',
          headers: { 'content-type': 'application/json' },
          body: JSON.stringify({
            agent: state.agent,
            imageProfile: selectedProfile,
            allowProfileFallback: !selectedProfile,
            prompt,
            size: $('labImageSize')?.value || null,
            count: 1
          })
        });
        result.innerHTML = '';
        const generated = data.results || [];
        const first = generated[0] || {};
        const usedProfile = first.imageProfileName || first.imageProfileId || first.model;
        if (usedProfile) {
          const meta = document.createElement('div');
          meta.className = 'lab-result-meta';
          meta.textContent = 'profile: ' + usedProfile + (first.model && first.model !== usedProfile ? ' / model: ' + first.model : '');
          result.appendChild(meta);
        }
        const paths = generated.map(item => item.relativePath).filter(Boolean);
        result.appendChild(paths.length ? createPreviewBox(paths) : document.createTextNode(t('labNoImageReturned')));
      } catch (err) {
        result.textContent = err.message || String(err);
      }
    }

    async function runLabTts() {
      const text = $('labTtsText')?.value.trim();
      if (!text) return;
      const result = $('labTtsResult');
      result.textContent = t('labGeneratingSpeech');
      try {
        const data = await api('/api/audio/speech', {
          method: 'POST',
          headers: { 'content-type': 'application/json' },
          body: JSON.stringify({
            agent: state.agent,
            ttsProfile: $('labTtsProfile')?.value || null,
            allowProfileFallback: !$('labTtsProfile')?.value,
            text,
            voice: $('labTtsVoice')?.value.trim() || null
          })
        });
        result.innerHTML = '';
        const audio = data.audio;
        const button = document.createElement('button');
        button.type = 'button';
        button.className = 'ghost audio-button';
        button.textContent = t('labPlayAudio');
        button.onclick = () => playAudio(audio, button, { showError: true });
        const path = document.createElement('div');
        path.className = 'muted-status';
        path.textContent = audio?.relativePath || '';
        result.append(button, path);
        if (effectiveTts()?.autoPlay) playAudio(audio, button, { showError: false });
      } catch (err) {
        result.textContent = err.message || String(err);
      }
    }

    async function runLabSttFile() {
      const result = $('labSttResult');
      result.textContent = t('labFileSttUnavailable');
    }

    async function toggleLabRecording() {
      const button = $('labSttRecord');
      const result = $('labSttResult');
      if (!button || !canUseStt()) return;
      if (state.labRecorder) {
        state.labRecorder.stop();
        return;
      }
      button.textContent = 'Stop';
      button.classList.add('recording');
        result.textContent = t('labRecording');
      try {
        const text = await recognizeSpeechOnce(function(recognition) { state.labRecorder = recognition; });
        result.textContent = text || t('labNoTranscript');
      } catch (err) {
        result.textContent = err.message || String(err);
      } finally {
        button.textContent = 'Record';
        button.classList.remove('recording');
        state.labRecorder = null;
      }
    }

    async function createAgentFromPrompt() {
      const name = prompt(t('newAgentPrompt'));
      if (!name) return;
      setAgentConfigState(t('loadingConfig'));
      const trimmed = name.trim();
      const data = await api('/api/agents', {
        method: 'POST',
        headers: { 'content-type': 'application/json' },
        body: JSON.stringify({ name: trimmed })
      });
      state.agent = data.agent ?? trimmed;
      state.session = null;
      state.sessionReadOnly = false;
      await loadAgents(false);
      await loadSessions();
      if (state.activeTab === 'agent') await loadAgentConfig();
      if (state.activeTab === 'schedule') await loadScheduledTasks();
    }

    async function deleteCurrentAgent() {
      if (!state.agent) return;
      if (!confirm(t('deleteConfirm') + '\n' + state.agent)) return;
      const deletedAgent = state.agent;
      const data = await api(`/api/agents?agent=${encodeURIComponent(state.agent)}`, { method: 'DELETE' });
      state.agents = data.agents ?? [];
      if (state.runtimeEventsAgent === deletedAgent) state.runtimeEventsAgent = state.agents[0]?.name ?? null;
      state.agent = state.agents[0]?.name ?? null;
      state.session = null;
      state.sessionReadOnly = false;
      state.sessions = [];
      await loadAgents();
      if (!state.agent) {
        if ($('sessionSelect')) $('sessionSelect').innerHTML = '';
        renderRuntimeEvents({ events: [], summary: {}, remaining: [] });
      }
    }

    async function uploadAgentIcon() {
      if (!state.agent) return;
      const input = $('agentIconInput');
      const file = input?.files?.[0];
      if (!file) return;
      try {
        const form = new FormData();
        form.append('agent', state.agent);
        form.append('icon', file);
        const res = await fetch('/api/agent-icon', { method: 'POST', body: form });
        if (!res.ok) throw new Error(await res.text());
        const data = await res.json();
        fillAgentConfig(data);
        await loadAgents(false);
        if (state.session) await loadSession();
        setAgentConfigState(t('saved') + ' ' + data.agent);
      } finally {
        if (input) input.value = '';
      }
    }

    async function loadAgents(reloadSession = true) {
      const data = await api('/api/agents');
      state.agents = data.agents;
      const selectors = [$('agentSelect'), $('agentConfigSelect'), $('scheduleAgentSelect'), $('skillsAgentSelect'), $('labAgentSelect'), $('memoryAgentSelect'), $('runtimeEventsAgentSelect')].filter(Boolean);
      for (const selector of selectors) selector.innerHTML = '';
      for (const agent of state.agents) {
        for (const selector of selectors) {
          const opt = document.createElement('option');
          opt.value = agent.name;
          opt.textContent = `${agent.displayName ?? agent.name} · ${agent.modelId}`;
          selector.appendChild(opt);
        }
      }
      if (!state.agents.length) {
        state.agent = null;
        state.runtimeEventsAgent = null;
        state.session = null;
        state.sessionReadOnly = false;
        state.sessions = [];
        if ($('sessionSelect')) $('sessionSelect').innerHTML = '';
        renderRuntimeEvents({ events: [], summary: {}, remaining: [] });
        showEmpty(t('noAgents'));
        clearAgentConfigView();
        return;
      }
      if (!state.agents.some(function(agent) { return agent.name === state.agent; })) state.agent = state.agents[0].name;
      if (!state.agents.some(function(agent) { return agent.name === state.runtimeEventsAgent; })) state.runtimeEventsAgent = state.agent;
      syncAgentSelectors();
      syncSoundCueSettingsToServer(loadSoundCueSettings(), { immediate: true });
      if (reloadSession) await loadSessions();
      if (state.activeTab === 'agent') await loadAgentConfig();
      if (state.activeTab === 'schedule') await loadScheduledTasks();
      if (state.activeTab === 'settings' || state.activeTab === 'chat') await loadMultiModalConfig();
      if (state.activeTab === 'lab') await loadLab();
    }

    function sessionDisplayTitle(session) {
      return session?.displayTitle || session?.sessionId || session?.id || '';
    }

    function isNotificationSession(session) {
      return !!session && (session.isReadOnly || String(session.kind || '').toLowerCase() === 'scheduled_notification');
    }

    function sessionListLabel(session) {
      const title = sessionDisplayTitle(session);
      const suffix = isNotificationSession(session) ? ' [' + t('sessionNoticeSuffix') + ']' : '';
      return title + suffix + ' - ' + (session.totalMessages || 0) + ' ' + t('messagesShort');
    }

    function sessionPickerLabel(session) {
      const title = sessionDisplayTitle(session);
      const id = session?.sessionId || session?.id || '';
      return title && id && title !== id ? title + ' - ' + id : (title || id);
    }

    async function loadSessions(options = {}) {
      if (!state.agent) return;
      const opts = typeof options === 'object' ? options : {};
      const data = await api(`/api/sessions?agent=${encodeURIComponent(state.agent)}`);
      state.sessions = data.sessions;
      $('sessionSelect').innerHTML = '';
      for (const session of state.sessions) {
        const opt = document.createElement('option');
        opt.value = session.id;
        opt.textContent = sessionListLabel(session);
        $('sessionSelect').appendChild(opt);
      }
      if (!state.sessions.length) {
        await createSession();
        return;
      }
      if (!state.sessions.some(function(session) { return session.id === state.session; })) state.session = state.sessions[0].id;
      $('sessionSelect').value = state.session;
      syncScheduleSessionOptions();
      if (opts.reloadCurrentSession !== false) await loadSession();
    }

    function syncScheduleSessionOptions() {
      const selector = $('scheduleTargetSessions');
      if (!selector) return;
      selector.innerHTML = '';
      for (const session of (state.sessions ?? [])) {
        if (isNotificationSession(session)) continue;
        const opt = document.createElement('option');
        opt.value = session.id;
        opt.textContent = sessionPickerLabel(session);
        selector.appendChild(opt);
      }
    }

    async function createSession() {
      const data = await api('/api/sessions', {
        method: 'POST',
        headers: { 'content-type': 'application/json' },
        body: JSON.stringify({ agent: state.agent })
      });
      state.session = data.sessionId;
      await loadSessions();
    }

    async function loadSession() {
      const data = await api(`/api/session?agent=${encodeURIComponent(state.agent)}&session=${encodeURIComponent(state.session)}`);
      $('model').textContent = data.agent.modelId;
      updateStats(data.session);
      renderTask(data.activeTask);
      $('title').textContent = agentDisplayName(data.agent.name);
      const currentTitle = data.session.displayTitle || data.session.sessionId;
      $('subtitle').textContent = t('sessionWord') + ' ' + currentTitle + (data.session.isReadOnly ? ' [' + t('sessionNoticeSuffix') + ']' : '');
      state.sessionReadOnly = !!data.session.isReadOnly;
      syncComposerState();
      state.lastMessageCount = data.messages?.length ?? 0;
      renderMessages(data.messages);
      state.scheduledNoticeKeys = collectScheduledNoticeKeys(data.messages || []);
      state.imageNoticeKeys = collectImageNoticeKeys(data.messages || []);
    }
    let chatPollTimer = null;
    function startChatPolling() {
      stopChatPolling();
      chatPollTimer = setInterval(async () => {
        if (!state.agent || !state.session || state.busy) return;
        try {
          const data = await api(`/api/session?agent=${encodeURIComponent(state.agent)}&session=${encodeURIComponent(state.session)}`);
          const currentCount = data.messages?.length ?? 0;
          state.sessionReadOnly = !!data.session?.isReadOnly;
          syncComposerState();
          if (currentCount !== state.lastMessageCount) {
            const noticeKeys = collectScheduledNoticeKeys(data.messages || []);
            const imageNoticeKeys = collectImageNoticeKeys(data.messages || []);
            const hasNewNotice = Array.from(noticeKeys).some(key => !state.scheduledNoticeKeys.has(key));
            const hasNewImageNotice = Array.from(imageNoticeKeys).some(key => !state.imageNoticeKeys.has(key));
            state.lastMessageCount = currentCount;
            updateStats(data.session);
            renderTask(data.activeTask);
            renderMessages(data.messages);
            state.scheduledNoticeKeys = noticeKeys;
            state.imageNoticeKeys = imageNoticeKeys;
            if (hasNewNotice) playSoundCue('reply_done');
            if (hasNewImageNotice) triggerHostNoticeContinuation();
          }
        } catch {}
      }, 5000);
    }
    function stopChatPolling() {
      if (chatPollTimer) { clearInterval(chatPollTimer); chatPollTimer = null; }
    }

    function updateStats(session) {
      const usage = session.contextUsage || 0;
      $('ctxText').textContent = `${usage}% · ${(session.tokens || 0).toLocaleString()} tok`;
      $('ctxFill').style.width = `${usage}%`;
      $('msgCount').textContent = session.totalMessages || 0;
      $('toolCount').textContent = session.toolMessagesCount || 0;
    }

    function commandNames(command) {
      return [command.name, ...(command.aliases || [])];
    }

    function commandToken(value = $('input').value) {
      const trimmed = String(value || '').trimStart();
      if (!trimmed.startsWith('/')) return '';
      return trimmed.split(/\s+/)[0].toLowerCase();
    }

    function commandDescription(command) {
      return t(command.descriptionKey, command.description ?? command.name);
    }

    function commandHelpMarkdown() {
      const lines = ['# ' + t('cmdHelpTitle')];
      commands.forEach(function(command) {
        const aliases = command.aliases.length ? ' (' + command.aliases.join(', ') + ')' : '';
        lines.push('- **' + command.name + '**' + aliases + ': ' + commandDescription(command));
      });
      return lines.join('\n');
    }

    function findCommand(token) {
      if (!token) return null;
      return commands.find(command => commandNames(command).some(name => name.toLowerCase() === token));
    }

    function commandMatches() {
      const token = commandToken();
      if (!token) return [];
      return commands.filter(command => commandNames(command).some(name => name.toLowerCase().startsWith(token)));
    }

    function hideCommandMenu() {
      const menu = $('commandMenu');
      if (!menu) return;
      menu.hidden = true;
      menu.classList.remove('active');
      menu.innerHTML = '';
    }

    function renderCommandMenu() {
      const menu = $('commandMenu');
      if (!menu) return;
      const token = commandToken();
      if (!token) { hideCommandMenu(); return; }
      const matches = commandMatches();
      state.commandIndex = Math.min(state.commandIndex, Math.max(matches.length - 1, 0));
      menu.innerHTML = '';
      if (!matches.length) {
        const empty = document.createElement('button');
        empty.type = 'button';
        empty.className = 'command-item active';
        empty.innerHTML = '<span class="command-name">' + escapeHtml(t('commandNoCommand')) + '</span><span class="command-desc">' + escapeHtml(t('commandNoCommandHint')) + '</span>';
        menu.appendChild(empty);
      } else {
        matches.forEach((command, index) => {
          const item = document.createElement('button');
          item.type = 'button';
          item.className = `command-item${index === state.commandIndex ? ' active' : ''}`;
          item.innerHTML = '<span class="command-name">' + escapeHtml(command.name) + '</span><span class="command-desc">' + escapeHtml(commandDescription(command)) + '</span>';
          item.addEventListener('mousedown', event => event.preventDefault());
          item.addEventListener('click', () => completeCommand(command));
          menu.appendChild(item);
        });
      }
      menu.hidden = false;
      menu.classList.add('active');
    }

    function completeCommand(command = commandMatches()[state.commandIndex]) {
      if (!command) return false;
      const input = $('input');
      input.value = `${command.name} `;
      input.focus();
      input.setSelectionRange(input.value.length, input.value.length);
      hideCommandMenu();
      $('hint').textContent = t('commandCompleteHint').replace('{command}', command.name);
      return true;
    }

    async function runSlashCommand(text) {
      const token = commandToken(text);
      if (!token) return false;
      const command = findCommand(token);
      if (!command) {
        $('hint').textContent = t('unknownCommand').replace('{command}', token);
        renderCommandMenu();
        return true;
      }
      if (state.busy) {
        $('hint').textContent = t('commandBusy');
        return true;
      }
      $('input').value = '';
      hideCommandMenu();
      const note = await command.run(text.slice(token.length).trim());
      if (note) $('hint').textContent = note;
      return true;
    }

    function setScheduleState(text) {
      const node = $('scheduleState');
      if (node) {
        node.textContent = text;
        if (text) node.dataset.dynamic = '1';
        else delete node.dataset.dynamic;
      }
    }
    function defaultScheduleJson() {
      return JSON.stringify({type: 'daily', time: '09:30'}, null, 2);
    }
    function browserTimeZone() {
      try { return Intl.DateTimeFormat().resolvedOptions().timeZone || ''; }
      catch { return ''; }
    }
    function toLocalDateTimeInputValue(date) {
      const d = date instanceof Date ? date : new Date(date);
      if (Number.isNaN(d.getTime())) return '';
      const pad = value => String(value).padStart(2, '0');
      return d.getFullYear() + '-' + pad(d.getMonth() + 1) + '-' + pad(d.getDate()) + 'T' + pad(d.getHours()) + ':' + pad(d.getMinutes());
    }
    function scheduleDateTimeInputValue(value) {
      const text = String(value || '').trim();
      if (!text) return toLocalDateTimeInputValue(new Date(Date.now() + 60 * 60 * 1000));
      if (/[zZ]$|[+-]\d{2}:?\d{2}$/.test(text)) return toLocalDateTimeInputValue(new Date(text));
      return text.length > 16 ? text.slice(0, 16) : text;
    }
    async function syncBrowserTimeZone() {
      const timeZone = browserTimeZone();
      if (!timeZone) return;
      try {
        state.userTimeZone = await api('/api/user-time-zone', { method: 'POST', headers: { 'content-type': 'application/json' }, body: JSON.stringify({ timeZone }) });
      } catch {}
    }
    function initScheduleRuleEditor() {
      document.querySelectorAll('[data-rule-type]').forEach(function(btn) {
        btn.addEventListener('click', function() {
          document.querySelectorAll('[data-rule-type]').forEach(function(b) { b.classList.remove('active'); });
          btn.classList.add('active');
          renderScheduleRuleFields(btn.dataset.ruleType);
          syncScheduleJsonFromFields();
        });
      });
    }
    function renderScheduleRuleFields(type) {
      const container = $('scheduleRuleFields');
      if (!container) return;
      container.innerHTML = '';
      if (type === 'daily') {
        container.innerHTML = '<div class="schedule-rule-row single"><input type="time" id="ruleDailyTime" value="09:30" /></div>';
      } else if (type === 'daily_times') {
        container.innerHTML = '<div class="schedule-rule-row single"><div id="ruleDailyTimesChips" style="display:flex;flex-wrap:wrap;gap:6px;margin-bottom:8px;"></div><div style="display:flex;gap:8px;align-items:center;"><input type="time" id="ruleDailyTimeInput" style="width:auto;min-height:36px;" /><button type="button" class="ghost" id="ruleAddTime" style="width:auto;min-height:36px;padding:0 14px;font-size:12px;">' + escapeHtml(t('scheduleRuleAdd')) + '</button></div></div>';
        const addBtn = $('ruleAddTime');
        const input = $('ruleDailyTimeInput');
        const chips = $('ruleDailyTimesChips');
        if (addBtn && input && chips) {
          let times = [];
          try { if (container.dataset.times) times = JSON.parse(container.dataset.times); } catch {}
          function renderChips() {
            chips.innerHTML = '';
            times.forEach(function(t, idx) {
              const chip = document.createElement('span');
              chip.className = 'schedule-rule-chip';
              chip.innerHTML = t + ' <button type="button" data-idx="' + idx + '">&times;</button>';
              chips.appendChild(chip);
            });
            container.dataset.times = JSON.stringify(times);
            syncScheduleJsonFromFields();
          }
          addBtn.addEventListener('click', function() {
            if (input.value && times.indexOf(input.value) === -1) {
              times.push(input.value);
              times.sort();
              renderChips();
            }
          });
          chips.addEventListener('click', function(e) {
            if (e.target.tagName === 'BUTTON') {
              const idx = parseInt(e.target.dataset.idx);
              times.splice(idx, 1);
              renderChips();
            }
          });
          container.renderChips = renderChips;
        }
      } else if (type === 'daily_window') {
        container.innerHTML = '<div class="schedule-rule-row"><input type="time" id="ruleWindowStart" value="09:00" /><input type="time" id="ruleWindowEnd" value="18:00" /></div><div class="schedule-rule-row single"><label style="color:var(--soft);font-size:12px;margin-bottom:4px;">' + escapeHtml(t('scheduleRuleInterval')) + '</label><input type="number" id="ruleInterval" value="60" min="1" max="1440" /></div>';
      } else if (type === 'once') {
        const today = toLocalDateTimeInputValue(new Date(Date.now() + 60 * 60 * 1000));
        container.innerHTML = '<div class="schedule-rule-row single"><input type="datetime-local" id="ruleOnceAt" value="' + today + '" /></div>';
      }
      container.querySelectorAll('input, select').forEach(function(el) {
        el.addEventListener('change', syncScheduleJsonFromFields);
        el.addEventListener('input', syncScheduleJsonFromFields);
      });
    }
    function syncScheduleJsonFromFields() {
      const type = document.querySelector('[data-rule-type].active')?.dataset.ruleType || 'daily';
      let schedule = { type: type };
      if (type === 'daily') {
        schedule.time = $('ruleDailyTime')?.value || '09:30';
      } else if (type === 'daily_times') {
        let times = [];
        try { const container = $('scheduleRuleFields'); if (container && container.dataset.times) times = JSON.parse(container.dataset.times); } catch {}
        schedule.times = times;
      } else if (type === 'daily_window') {
        schedule.windowStart = $('ruleWindowStart')?.value || '09:00';
        schedule.windowEnd = $('ruleWindowEnd')?.value || '18:00';
        schedule.intervalMinutes = parseInt($('ruleInterval')?.value || '60', 10);
      } else if (type === 'once') {
        schedule.runAt = $('ruleOnceAt')?.value || toLocalDateTimeInputValue(new Date(Date.now() + 60 * 60 * 1000));
      }
      $('scheduleJson').value = JSON.stringify(schedule);
    }
    function setScheduleRuleType(type, values) {
      const btn = document.querySelector('[data-rule-type="' + type + '"]');
      if (btn) {
        document.querySelectorAll('[data-rule-type]').forEach(function(b) { b.classList.remove('active'); });
        btn.classList.add('active');
      }
      renderScheduleRuleFields(type);
      if (type === 'daily' && values && values.time) {
        const el = $('ruleDailyTime');
        if (el) el.value = values.time;
      } else if (type === 'daily_times' && values && Array.isArray(values.times)) {
        const container = $('scheduleRuleFields');
        if (container) {
          container.dataset.times = JSON.stringify(values.times);
          if (container.renderChips) container.renderChips();
          else {
            const chips = $('ruleDailyTimesChips');
            if (chips) {
              chips.innerHTML = '';
              values.times.forEach(function(t, idx) {
                const chip = document.createElement('span');
                chip.className = 'schedule-rule-chip';
                chip.innerHTML = t + ' <button type="button" data-idx="' + idx + '">&times;</button>';
                chips.appendChild(chip);
              });
              container.dataset.times = JSON.stringify(values.times);
            }
          }
        }
      } else if (type === 'daily_window' && values) {
        const ws = $('ruleWindowStart'), we = $('ruleWindowEnd'), ri = $('ruleInterval');
        if (ws && values.windowStart) ws.value = values.windowStart;
        if (we && values.windowEnd) we.value = values.windowEnd;
        if (ri && values.intervalMinutes) ri.value = String(values.intervalMinutes);
      } else if (type === 'once' && values && values.runAt) {
        const el = $('ruleOnceAt');
        if (el) el.value = scheduleDateTimeInputValue(values.runAt);
      }
      syncScheduleJsonFromFields();
    }
    function parseScheduleJson() {
      try {
        const schedule = JSON.parse($('scheduleJson').value || '{}');
        const type = schedule.type || 'daily';
        setScheduleRuleType(type, schedule);
      } catch {
        setScheduleRuleType('daily', { time: '09:30' });
      }
    }
    function resetScheduleForm() {
      if (!$('scheduleForm')) return;
      $('scheduleTaskId').value = '';
      $('scheduleTitle').value = '';
      $('scheduleContent').value = '';
      $('scheduleStatus').value = 'enabled';
      $('scheduleTargetMode').value = state.sessionReadOnly ? 'notification_session' : 'created_session';
      delete $('scheduleTargetMode').dataset.notificationSessionId;
      $('scheduleJson').value = JSON.stringify({type: 'daily', time: '09:30'});
      parseScheduleJson();
      syncScheduleSessionOptions();
      setScheduleState(t('scheduleStateNew'));
    }
    function makeScheduleAction(label, action, id, className) {
      const button = document.createElement('button');
      button.type = 'button';
      button.className = className ?? 'ghost';
      button.dataset.action = action;
      button.dataset.id = id;
      button.textContent = label;
      return button;
    }
    function describeSchedule(schedule) {
      if (!schedule) return '-';
      const zh = state.lang === 'zh';
      if (schedule.type === 'once') return (zh ? '单次 ' : 'Once ') + (schedule.runAt ?? '?');
      if (schedule.type === 'daily') return (zh ? '每天 ' : 'Daily ') + (schedule.time ?? '?');
      if (schedule.type === 'daily_times') return (zh ? '每天 ' : 'Daily ') + (schedule.times ?? []).join(', ');
      if (schedule.type === 'daily_window') return (zh ? '时段 ' : 'Window ') + (schedule.windowStart ?? '?') + '-' + (schedule.windowEnd ?? '?') + (zh ? ' 每' : ' every ') + (schedule.intervalMinutes ?? '?') + 'm';
      if (schedule.type === 'daily_count') return (zh ? '每日' : 'Count ') + (schedule.countPerDay ?? '?') + (zh ? '次 从' : ' from ') + (schedule.startTime ?? schedule.time ?? '?');
      if (schedule.type === 'interval') return (zh ? '每' : 'Every ') + (schedule.intervalMinutes ?? '?') + 'm';
      if (schedule.type === 'after_count') return (zh ? '限次' : 'After count, max ') + (schedule.maxRuns ?? '?');
      return schedule.type ?? 'schedule';
    }
    function formatScheduleDate(value, timeZone) {
      if (!value) return t('scheduleNone');
      try {
        const d = new Date(value);
        const locale = state.lang === 'zh' ? 'zh-CN' : 'en-US';
        if (timeZone) {
          return d.toLocaleString(locale, { timeZone: timeZone, year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false }) + ' (' + timeZone + ')';
        }
        return d.toLocaleString(locale, { hour12: false });
      } catch { return String(value); }
    }
    function formatRunStatus(status) {
      const value = String(status || '').toLowerCase();
      if (value === 'succeeded' || value === 'success') return t('scheduleRunStatusSucceeded');
      if (value === 'failed' || value === 'error') return t('scheduleRunStatusFailed');
      if (value === 'running') return t('scheduleRunStatusRunning');
      if (value === 'canceled' || value === 'cancelled') return t('scheduleRunStatusCanceled');
      if (value === 'interrupted') return t('scheduleRunStatusInterrupted');
      if (value === 'stalled') return t('scheduleRunStatusStalled');
      if (!value) return t('scheduleRunStatusUnknown');
      return status;
    }
    function localizedScheduledTaskTitle(task) {
      const taskId = String(task?.taskId || '');
      if (taskId === 'sched_system_memory_org') return t('scheduleSystemMemoryTitle');
      if (taskId === 'sched_system_skill_org') return t('scheduleSystemSkillTitle');
      return task?.title || taskId || '';
    }
    function localizedScheduledTaskContent(task) {
      const taskId = String(task?.taskId || '');
      if (taskId === 'sched_system_memory_org') return t('scheduleSystemMemoryContent');
      if (taskId === 'sched_system_skill_org') return t('scheduleSystemSkillContent');
      return task?.content || '';
    }
    function isSystemScheduledTask(taskOrId) {
      const taskId = typeof taskOrId === 'string' ? taskOrId : String(taskOrId?.taskId || '');
      return !!(typeof taskOrId === 'object' && taskOrId?.isSystem) || taskId === 'sched_system_memory_org' || taskId === 'sched_system_skill_org';
    }
    function findScheduledTaskInState(id) {
      if (state.scheduledSelected?.task?.taskId === id) return state.scheduledSelected.task;
      return (state.scheduledTasks?.items || []).find(function(task) { return task.taskId === id; }) || null;
    }
    function renderScheduledTasks(data) {
      const list = $('scheduleList');
      if (!list) return;
      list.innerHTML = '';
      const items = data.items ?? [];
      const page = data.page ?? 1;
      const pageSize = data.pageSize ?? state.schedulePageSize;
      const total = data.total ?? 0;
      const totalPages = Math.max(1, Math.ceil(total / pageSize));
      $('schedulePageInfo').textContent = page + '/' + totalPages + '·' + total;
      if (!items.length) { const empty = document.createElement('div'); empty.className = 'task-empty'; empty.textContent = t('scheduleNoTasks'); list.appendChild(empty); return; }
      for (const task of items) {
        const card = document.createElement('div');
        card.className = 'schedule-item';
        // Header: title + badge
        const header = document.createElement('div');
        header.className = 'schedule-item-header';
        const titleBlock = document.createElement('div');
        titleBlock.className = 'schedule-title-block';
        const badge = document.createElement('span');
        badge.className = 'schedule-badge ' + (task.status ?? 'enabled');
        badge.textContent = t('scheduleStatus' + (task.status ?? 'enabled').replace(/^\w/, c => c.toUpperCase()), task.status ?? 'enabled');
        const title = document.createElement('strong');
        title.textContent = localizedScheduledTaskTitle(task);
        const meta = document.createElement('div');
        meta.className = 'schedule-meta';
        meta.textContent = describeSchedule(task.schedule);
        titleBlock.append(badge, title, meta);
        header.appendChild(titleBlock);
        card.appendChild(header);
        // Preview
        const preview = document.createElement('p');
        preview.className = 'schedule-preview';
        preview.textContent = localizedScheduledTaskContent(task);
        card.appendChild(preview);
        const recoveryStatus = String(task.lastRunStatus || '').toLowerCase();
        const needsRecovery = Boolean(task.stalledUntil) || ['stalled','interrupted','failed','canceled','cancelled'].includes(recoveryStatus);
        // Meta grid
        const grid = document.createElement('div');
        grid.className = 'schedule-meta-grid';
        const metaCells = [[t('scheduleNextRun'), formatScheduleDate(task.nextRunAt, task.timeZone)],[t('scheduleLastRun'), formatScheduleDate(task.lastRunAt, task.timeZone)],[t('scheduleRunCount'), String(task.runCount ?? 0)],[t('scheduleFailCount'), String(task.failureCount ?? 0)]];
        if (task.stalledUntil) metaCells.push([t('scheduleStalledUntil'), formatScheduleDate(task.stalledUntil, task.timeZone)]);
        if (task.activeRunLastHeartbeatAt) metaCells.push([t('scheduleHeartbeat'), formatScheduleDate(task.activeRunLastHeartbeatAt, task.timeZone)]);
        for (const cell of metaCells) {
          const item = document.createElement('div');
          item.className = 'schedule-meta-cell';
          const label = document.createElement('span');
          label.textContent = cell[0];
          const value = document.createElement('b');
          value.textContent = cell[1];
          item.append(label, value);
          grid.appendChild(item);
        }
        card.appendChild(grid);
        // Actions
        const actions = document.createElement('div');
        actions.className = 'schedule-item-actions';
        actions.append(makeScheduleAction(t('scheduleActionEdit'), 'edit', task.taskId), makeScheduleAction(t('scheduleActionRead'), 'read', task.taskId));
        if (!isSystemScheduledTask(task)) actions.append(makeScheduleAction(t('scheduleActionTest'), 'do', task.taskId));
        if (needsRecovery) actions.append(makeScheduleAction(t('scheduleActionRetry'), 'retry', task.taskId), makeScheduleAction(t('scheduleActionRepairRetry'), 'repair-retry', task.taskId));
        actions.append(makeScheduleAction(t('scheduleActionDelete'), 'delete', task.taskId, 'danger'));
        card.appendChild(actions);
        list.appendChild(card);
      }
    }
    async function loadScheduledTasks(page = state.schedulePage) {
      if (!state.agent) return;
      if (!$('scheduleList')) return;
      state.schedulePage = page;
      setScheduleState(t('scheduleStateLoading'));
      const params = new URLSearchParams({agent: state.agent, page: state.schedulePage, pageSize: state.schedulePageSize});
      const data = await api('/api/scheduled-tasks?' + params.toString());
      state.scheduledTasks = data;
      renderScheduledTasks(data);
      setScheduleState('');
    }

    function scheduleTargets() {
      const mode = $('scheduleTargetMode').value;
      if (mode === 'none') return [];
      if (mode === 'all_agent_sessions') return [{ type: 'all_agent_sessions' }];
      if (mode === 'notification_session') {
        const sessionId = $('scheduleTargetMode').dataset.notificationSessionId || '';
        return [sessionId ? { type: 'notification_session', sessionId } : { type: 'notification_session' }];
      }
      if (mode === 'session') return Array.from($('scheduleTargetSessions').selectedOptions).map(function(option) { return { type: 'session', sessionId: option.value }; });
      return [{ type: 'created_session' }];
    }
    function schedulePayload() {
      const schedule = JSON.parse($('scheduleJson').value || '{"type":"daily","time":"09:30"}');
      const payload = { agent: state.agent, title: $('scheduleTitle').value.trim(), content: $('scheduleContent').value.trim(), timeZone: browserTimeZone(), schedule: schedule, targets: scheduleTargets() };
      const taskId = $('scheduleTaskId').value.trim();
      if (taskId) { payload.taskId = taskId; payload.status = $('scheduleStatus').value; }
      else payload.createdFromSession = state.session;
      return payload;
    }
    function fillScheduleForm(task) {
      if (!task) { resetScheduleForm(); return; }
      const isSystemTask = task.isSystem || task.taskId === 'sched_system_memory_org' || task.taskId === 'sched_system_skill_org';
      $('scheduleTaskId').value = task.taskId;
      $('scheduleTitle').value = isSystemTask ? localizedScheduledTaskTitle(task) : (task.title ?? '');
      $('scheduleContent').value = isSystemTask ? localizedScheduledTaskContent(task) : (task.content ?? '');
      $('scheduleStatus').value = task.status ?? 'enabled';
      $('scheduleJson').value = JSON.stringify(task.schedule ?? {});
      parseScheduleJson();
      const targets = task.targets ?? [];
      $('scheduleTargetMode').value = (targets[0]?.type ?? 'none');
      if (targets[0]?.type === 'notification_session' && targets[0]?.sessionId) $('scheduleTargetMode').dataset.notificationSessionId = targets[0].sessionId;
      else delete $('scheduleTargetMode').dataset.notificationSessionId;
      syncScheduleSessionOptions();
      for (const option of $('scheduleTargetSessions').options) option.selected = targets.some(function(target) { return target.sessionId === option.value; });
    }
    function renderScheduleHistory(runs, timeZone) {
      const node = $('scheduleHistory');
      if (!node) return;
      const items = runs ?? [];
      if (!items.length) { node.innerHTML = '<div class="task-empty">' + escapeHtml(t('scheduleHistoryEmpty')) + '</div>'; return; }
      node.innerHTML = '';
      for (const run of items) {
        const entry = document.createElement('div');
        entry.className = 'schedule-history-entry';
        const head = document.createElement('div');
        head.className = 'schedule-history-head';
        const status = document.createElement('span');
        const runStatus = String(run.status || '').toLowerCase();
        const statusClass = (runStatus === 'succeeded' || runStatus === 'success') ? 'success' : (['error','failed','canceled','cancelled','interrupted','stalled'].includes(runStatus)) ? 'error' : 'running';
        status.className = 'schedule-history-status ' + statusClass;
        status.textContent = (String(run.trigger || '').toLowerCase() === 'manual' ? t('scheduleManualTestLabel') + ' - ' : '') + formatRunStatus(run.status);
        const time = document.createElement('span');
        time.className = 'schedule-history-time';
        const started = formatScheduleDate(run.startedAt, timeZone);
        const finished = formatScheduleDate(run.finishedAt, timeZone);
        time.textContent = started + (finished && finished !== t('scheduleNone') ? ' → ' + finished : '');
        head.append(status, time);
        entry.appendChild(head);
        if (run.error) {
          const err = document.createElement('div');
          err.className = 'schedule-history-error';
          err.textContent = run.error;
          entry.appendChild(err);
        }
        if (run.diagnostic) {
          const diag = document.createElement('div');
          diag.className = 'schedule-history-error';
          diag.textContent = t('scheduleDiagnostic') + ': ' + run.diagnostic;
          entry.appendChild(diag);
        }
        if (run.output) {
          const out = document.createElement('div');
          out.className = 'schedule-history-output';
          out.textContent = run.output;
          entry.appendChild(out);
        }
        if (run.events && run.events.length > 0) {
          const events = document.createElement('div');
          events.className = 'schedule-history-events';
          events.style.cssText = 'margin-top:6px;font-size:11px;color:var(--faint);';
          const evSummary = run.events.slice(0, 3).map(function(e) { return (e.type || 'event') + (e.toolName ? ':' + e.toolName : ''); }).join(' · ');
          events.textContent = evSummary + (run.events.length > 3 ? ' · +' + (run.events.length - 3) + ' ' + t('scheduleMoreSuffix') : '');
          entry.appendChild(events);
        }
        node.appendChild(entry);
      }
    }
    async function readScheduledTask(id) {
      const params = new URLSearchParams({ agent: state.agent, taskId: id, take: 20 });
      const data = await api('/api/scheduled-task?' + params.toString());
      state.scheduledSelected = data;
      fillScheduleForm(data.task);
      renderScheduleHistory(data.runs, data.task?.timeZone);
      setScheduleState('');
    }
    async function saveScheduledTask() {
      const payload = schedulePayload();
      const editing = Boolean(payload.taskId);
      const task = await api('/api/scheduled-tasks', { method: editing ? 'PUT' : 'POST', headers: { 'content-type': 'application/json' }, body: JSON.stringify(payload) });
      fillScheduleForm(task);
      await loadScheduledTasks();
      if (task.taskId) {
        const params = new URLSearchParams({ agent: state.agent, taskId: task.taskId, take: 20 });
        const data = await api('/api/scheduled-task?' + params.toString());
        renderScheduleHistory(data.runs, data.task?.timeZone);
      }
      setScheduleState(t('saved'));
    }
    async function deleteScheduledTask(id) {
      if (!confirm(t('scheduleConfirmDelete'))) return;
      const params = new URLSearchParams({ agent: state.agent, taskId: id });
      await api('/api/scheduled-tasks?' + params.toString(), { method: 'DELETE' });
      resetScheduleForm();
      await loadScheduledTasks();
    }
    let scheduleTestPollTimer = null;
    function setScheduleTestOverlay(active, title, stage, detail) {
      const overlay = $('scheduleTestOverlay');
      if (!overlay) return;
      overlay.classList.toggle('active', !!active);
      overlay.setAttribute('aria-hidden', active ? 'false' : 'true');
      if (title && $('scheduleTestTitle')) $('scheduleTestTitle').textContent = title;
      if (stage && $('scheduleTestStage')) $('scheduleTestStage').textContent = stage;
      if (detail && $('scheduleTestDetail')) $('scheduleTestDetail').textContent = detail;
    }
    function stopScheduleTestPolling() {
      if (scheduleTestPollTimer) {
        clearInterval(scheduleTestPollTimer);
        scheduleTestPollTimer = null;
      }
    }
    async function pollScheduleTestProgress(id) {
      try {
        const params = new URLSearchParams({ agent: state.agent, taskId: id, take: 5 });
        const data = await api('/api/scheduled-task?' + params.toString());
        const task = data.task || {};
        const runs = data.runs || [];
        const activeRun = runs.find(function(run) { return run.runId && run.runId === task.activeRunId; }) || runs[0] || {};
        const stage = task.activeRunHeartbeatMessage || activeRun.heartbeatMessage || task.activeRunHeartbeatKind || activeRun.heartbeatKind || t('scheduleTestingStage');
        setScheduleTestOverlay(true, t('scheduleTestingTitle') + ': ' + localizedScheduledTaskTitle(task), stage, t('scheduleTestingDeliver'));
      } catch {}
    }
    async function runScheduledTaskOnce(id) {
      const task = findScheduledTaskInState(id);
      if (isSystemScheduledTask(task || id)) {
        setScheduleState(t('scheduleSystemTestBlocked'));
        return;
      }
      if (!confirm(t('scheduleConfirmRun'))) return;
      stopScheduleTestPolling();
      const title = task ? localizedScheduledTaskTitle(task) : id;
      setScheduleTestOverlay(true, t('scheduleTestingTitle') + ': ' + title, t('scheduleTestingStage'), t('scheduleTestingDeliver'));
      scheduleTestPollTimer = setInterval(function() { pollScheduleTestProgress(id); }, 1500);
      pollScheduleTestProgress(id);
      try {
        const run = await api('/api/scheduled-tasks/do?deliver=true', { method: 'POST', headers: { 'content-type': 'application/json' }, body: JSON.stringify({ agent: state.agent, taskId: id, reason: 'manual-ui-test' }) });
        const ok = String(run?.status || '').toLowerCase() === 'succeeded';
        setScheduleTestOverlay(true, ok ? t('scheduleTestDone') : t('scheduleTestFailed') + ': ' + (run?.error || run?.status || ''), run?.heartbeatMessage || run?.output || run?.error || '', t('scheduleTestingDeliver'));
        setScheduleState(ok ? t('scheduleTestDone') : t('scheduleTestFailed'));
      } catch (err) {
        setScheduleTestOverlay(true, t('scheduleTestFailed'), err.message || String(err), t('scheduleTestingDeliver'));
        setScheduleState(t('scheduleTestFailed') + ': ' + (err.message || String(err)));
      } finally {
        stopScheduleTestPolling();
        try {
          await readScheduledTask(id);
          await loadScheduledTasks();
        } catch {}
        setTimeout(function() { setScheduleTestOverlay(false); }, 1400);
      }
    }
    async function retryScheduledTask(id) {
      if (!confirm(t('scheduleConfirmRetry'))) return;
      await api('/api/scheduled-tasks/retry', { method: 'POST', headers: { 'content-type': 'application/json' }, body: JSON.stringify({ agent: state.agent, taskId: id, reason: 'manual-ui-retry' }) });
      await readScheduledTask(id);
      await loadScheduledTasks();
    }
    async function repairRetryScheduledTask(id) {
      if (!confirm(t('scheduleConfirmRepairRetry'))) return;
      const repaired = await api('/api/scheduled-tasks/repair-retry', { method: 'POST', headers: { 'content-type': 'application/json' }, body: JSON.stringify({ agent: state.agent, taskId: id, reason: 'manual-ui-repair-retry' }) });
      if (repaired?.taskId) await readScheduledTask(repaired.taskId);
      await loadScheduledTasks();
    }

    function setSkillsState(text, dynamic = Boolean(text)) {
      const node = $('skillsState');
      if (node) {
        node.textContent = text;
        if (dynamic) node.dataset.dynamic = '1';
        else delete node.dataset.dynamic;
      }
    }
    function renderSkillsList(data) {
      const list = $('skillsList');
      if (!list) return;
      if (!data.skills || data.skills.length === 0) {
        list.innerHTML = '<div class="skill-empty">' + escapeHtml(t('skillsEmpty')) + '</div>';
        return;
      }
      list.innerHTML = '';
      for (const skill of data.skills) {
        const card = document.createElement('div');
        card.className = 'skill-item' + (state.selectedSkillId === skill.id ? ' active' : '');
        card.dataset.id = skill.id;
        const icon = document.createElement('div');
        icon.className = 'skill-item-icon';
        icon.textContent = '🎯';
        const body = document.createElement('div');
        body.className = 'skill-item-body';
        const name = document.createElement('div');
        name.className = 'skill-item-name';
        name.textContent = skill.name;
        const desc = document.createElement('div');
        desc.className = 'skill-item-desc';
        desc.textContent = skill.description || t('noDescription');
        body.append(name, desc);
        if (skill.tags && skill.tags.length > 0) {
          const tags = document.createElement('div');
          tags.className = 'skill-item-tags';
          for (const tag of skill.tags) {
            const span = document.createElement('span');
            span.className = 'skill-tag';
            span.textContent = tag;
            tags.appendChild(span);
          }
          body.appendChild(tags);
        }
        const actions = document.createElement('div');
        actions.className = 'skill-item-actions';
        const actionRow = document.createElement('div');
        actionRow.className = 'skill-action-row';
        actionRow.append(makeScheduleAction(t('skillsActionLoad'), 'load', skill.id), makeScheduleAction(t('skillsActionExport'), 'export', skill.id), makeScheduleAction(t('skillsActionDelete'), 'delete', skill.id, 'danger'));
        const validateButton = makeScheduleAction(t('skillsActionValidate'), 'validate', skill.id);
        validateButton.classList.add('skill-validate-button');
        actions.append(actionRow, validateButton);
        card.append(icon, body, actions);
        list.appendChild(card);
      }
    }
    async function loadSkills() {
      if (!state.agent) return;
      if (!$('skillsList')) return;
      setSkillsState(t('skillsStateLoading'));
      try {
        const data = await api('/api/skills?agent=' + encodeURIComponent(state.agent));
        state.skills = data;
        renderSkillsList(data);
        setSkillsState('');
      } catch (err) {
        setSkillsState(t('skillsLoadFailedPrefix') + ': ' + err.message);
      }
    }
    async function readSkill(id) {
      try {
        const data = await api('/api/skill?agent=' + encodeURIComponent(state.agent) + '&skillId=' + encodeURIComponent(id));
        state.selectedSkillId = id;
        $('skillId').value = data.id;
        $('skillName').value = data.name || '';
        $('skillDescription').value = data.description || '';
        $('skillTags').value = (data.tags || []).join(', ');
        $('skillContent').value = data.content || '';
        state.selectedSkillValidationReport = data.validationReport || null;
        state.selectedSkillImportReport = data.importReport || null;
        updateSkillPreview();
        setSkillsState(t('skillsLoadedPrefix') + ': ' + data.name);
        renderSkillsList(state.skills);
      } catch (err) {
        setSkillsState(t('skillsReadFailedPrefix') + ': ' + err.message);
      }
    }
    async function saveSkill() {
      const id = $('skillId').value.trim();
      const payload = {
        agent: state.agent,
        name: $('skillName').value.trim(),
        description: $('skillDescription').value.trim(),
        tags: $('skillTags').value.split(',').map(function(t) { return t.trim(); }).filter(function(t) { return t; }),
        content: $('skillContent').value
      };
      if (id) {
        payload.id = id;
        await api('/api/skills?agent=' + encodeURIComponent(state.agent), { method: 'PUT', headers: { 'content-type': 'application/json' }, body: JSON.stringify(payload) });
        state.selectedSkillValidationReport = null;
        state.selectedSkillImportReport = null;
        setSkillsState(t('skillsUpdated'));
      } else {
        await api('/api/skills?agent=' + encodeURIComponent(state.agent), { method: 'POST', headers: { 'content-type': 'application/json' }, body: JSON.stringify(payload) });
        setSkillsState(t('skillsCreated'));
        resetSkillForm();
      }
      await loadSkills();
    }
    async function deleteSkill(id) {
      if (!confirm(t('skillsDeleteConfirm'))) return;
      await api('/api/skills?agent=' + encodeURIComponent(state.agent) + '&skillId=' + encodeURIComponent(id), { method: 'DELETE' });
      if (state.selectedSkillId === id) resetSkillForm();
      await loadSkills();
    }
    function skillExportFileName(response, fallbackId) {
      const disposition = response.headers.get('content-disposition') || '';
      const utfMatch = disposition.match(/filename\*=UTF-8''([^;]+)/i);
      if (utfMatch) {
        try { return decodeURIComponent(utfMatch[1].trim().replace(/^"|"$/g, '')); } catch {}
      }
      const match = disposition.match(/filename="?([^";]+)"?/i);
      if (match) return match[1].trim();
      return (fallbackId || 'skill') + '.zip';
    }
    async function exportSkill(id) {
      if (!id) { setSkillsState(t('skillsSelectFirst')); return; }
      const res = await fetch('/api/skills/export?agent=' + encodeURIComponent(state.agent) + '&skillId=' + encodeURIComponent(id));
      if (!res.ok) throw new Error(await res.text());
      const blob = await res.blob();
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = skillExportFileName(res, id);
      document.body.appendChild(link);
      link.click();
      link.remove();
      setTimeout(function() { try { URL.revokeObjectURL(url); } catch {} }, 1200);
      setSkillsState(t('skillsExported') + ': ' + id);
    }
    function resetSkillForm() {
      state.selectedSkillId = null;
      state.selectedSkillValidationReport = null;
      state.selectedSkillImportReport = null;
      $('skillId').value = '';
      $('skillName').value = '';
      $('skillDescription').value = '';
      $('skillTags').value = '';
      $('skillContent').value = '';
      setNodeText('#skillsPreviewTitle', t('skillsPreviewTitle'));
      $('skillPreview').innerHTML = '<span class="skill-empty">' + escapeHtml(t('skillsEmptyPreview')) + '</span>';
      setSkillsState(t('skillsState'), false);
      if (state.skills) renderSkillsList(state.skills);
    }
    function updateSkillPreview() {
      const content = $('skillContent').value;
      const preview = $('skillPreview');
      if (!preview) return;
      if (state.selectedSkillValidationReport && $('skillId').value.trim()) {
        setNodeText('#skillsPreviewTitle', t('skillsValidationReportTitle'));
        preview.innerHTML = '<div style="white-space:pre-wrap;">' + escapeHtml(state.selectedSkillValidationReport) + '</div>';
        return;
      }
      setNodeText('#skillsPreviewTitle', t('skillsContentPreviewTitle'));
      if (!content.trim()) {
        preview.innerHTML = '<span class="skill-empty">' + escapeHtml(t('skillsEmptyPreview')) + '</span>';
        return;
      }
      const note = state.selectedSkillId ? '<div class="muted-status" style="margin-bottom:8px;">' + escapeHtml(t('skillsNoValidationReport')) + '</div>' : '';
      preview.innerHTML = note + '<div style="white-space:pre-wrap;">' + escapeHtml(content) + '</div>';
    }

    function setMemoryState(text) {
      const node = $('memoryState');
      if (node) {
        node.textContent = text;
        if (text) node.dataset.dynamic = '1';
        else delete node.dataset.dynamic;
      }
    }
    function switchMemoryTab(tab) {
      tab = ['core', 'longterm', 'vector'].includes(tab) ? tab : 'core';
      state.memoryTab = tab;
      document.querySelectorAll('[data-memory-tab]').forEach(function(btn) {
        btn.classList.toggle('active', btn.dataset.memoryTab === tab);
      });
      $('memoryCoreView').classList.toggle('active', tab === 'core');
      $('memoryLongtermView').classList.toggle('active', tab === 'longterm');
      $('memoryVectorView').classList.toggle('active', tab === 'vector');
      if (tab === 'longterm') loadLongTermMemory();
      if (tab === 'vector') loadVectorMemory().catch(function(err) { setMemoryState(err.message); });
    }
    async function loadMemory() {
      if (!state.agent) return;
      setMemoryState(t('memoryStateLoading'));
      try {
        const data = await api('/api/memory?agent=' + encodeURIComponent(state.agent));
        $('memoryUserMd').value = data.userMd || '';
        $('memoryIdentityMd').value = data.identityMd || '';
        $('memoryHotMemory').value = data.hotMemory || '';
        $('memoryCoreMemory').value = data.coreMemory || '';
        state.memoryItems = data.longTermItems || [];
        await loadMemorySnapshots();
        if (state.memoryTab === 'longterm') await loadLongTermMemory();
        if (state.memoryTab === 'vector') await loadVectorMemory();
        setMemoryState('');
      } catch (err) {
        setMemoryState(err.message);
      }
    }
    async function saveCoreMemory() {
      if (!state.agent) return;
      const payload = {
        agent: state.agent,
        userMd: $('memoryUserMd').value,
        identityMd: $('memoryIdentityMd').value,
        hotMemory: $('memoryHotMemory').value,
        coreMemory: $('memoryCoreMemory').value
      };
      await api('/api/memory', { method: 'PUT', headers: { 'content-type': 'application/json' }, body: JSON.stringify(payload) });
      state.memoryVectorAtlas = null;
      setMemoryState(t('saved'));
      if (state.memoryTab === 'vector') await loadVectorMemory();
      setTimeout(function() { setMemoryState(''); }, 1500);
    }
    async function loadMemorySnapshots() {
      if (!state.agent) return;
      const data = await api('/api/memory/snapshots?agent=' + encodeURIComponent(state.agent));
      state.memorySnapshots = data.snapshots || [];
      renderMemorySnapshots();
    }
    function renderMemorySnapshots() {
      const select = $('memorySnapshotSelect');
      const restore = $('memorySnapshotRestore');
      if (!select) return;
      select.innerHTML = '';
      const snapshots = state.memorySnapshots || [];
      if (!snapshots.length) {
        const opt = document.createElement('option');
        opt.value = '';
        opt.textContent = t('memorySnapshotEmpty');
        select.appendChild(opt);
        if (restore) restore.disabled = true;
        return;
      }

      for (const snapshot of snapshots) {
        const opt = document.createElement('option');
        opt.value = snapshot.id || '';
        const created = snapshot.createdAt ? new Date(snapshot.createdAt).toLocaleString() : '';
        opt.textContent = (snapshot.id || 'snapshot') + (created ? ' - ' + created : '');
        select.appendChild(opt);
      }
      if (restore) restore.disabled = false;
    }
    async function restoreMemorySnapshot() {
      if (!state.agent) return;
      const snapshotId = $('memorySnapshotSelect')?.value || '';
      if (!snapshotId) return;
      if (!confirm(t('memorySnapshotConfirm'))) return;
      await api('/api/memory/restore-snapshot', {
        method: 'POST',
        headers: { 'content-type': 'application/json' },
        body: JSON.stringify({ agent: state.agent, snapshotId })
      });
      state.memoryVectorAtlas = null;
      setMemoryState(t('memorySnapshotRestored'));
      await loadMemory();
    }
    async function loadLongTermMemory(page = state.memoryPage) {
      if (!state.agent) return;
      state.memoryPage = page;
      const startDate = $('memoryLtmStartDate')?.value || '';
      const endDate = $('memoryLtmEndDate')?.value || '';
      const params = new URLSearchParams({ agent: state.agent, page: state.memoryPage, pageSize: state.memoryPageSize });
      if (startDate) params.set('startDate', startDate);
      if (endDate) params.set('endDate', endDate);
      const data = await api('/api/memory/long-term?' + params.toString());
      state.memoryItems = data;
      renderLongTermMemory(data);
    }
    function renderLongTermMemory(data) {
      const list = $('memoryLtmList');
      if (!list) return;
      list.innerHTML = '';
      const items = data.items ?? [];
      const page = data.page ?? 1;
      const pageSize = data.pageSize ?? state.memoryPageSize;
      const total = data.total ?? 0;
      const totalPages = Math.max(1, Math.ceil(total / pageSize));
      if (!items.length) {
        const empty = document.createElement('div');
        empty.className = 'task-empty';
        empty.textContent = t('memoryLtmEmpty');
        list.appendChild(empty);
      } else {
        for (const item of items) {
          const card = document.createElement('div');
          card.className = 'memory-item';
          card.dataset.id = item.id || '';
          const header = document.createElement('div');
          header.className = 'memory-item-header';
          const id = document.createElement('span');
          id.className = 'memory-item-id';
          id.textContent = item.id || '';
          const time = document.createElement('span');
          time.className = 'memory-item-time';
          time.textContent = item.timestamp ? new Date(item.timestamp).toLocaleDateString() : '';
          header.append(id, time);
          const content = document.createElement('div');
          content.className = 'memory-item-content';
          content.textContent = item.content || '';
          card.append(header, content);
          card.addEventListener('click', function() {
            document.querySelectorAll('.memory-item').forEach(el => el.classList.remove('active'));
            card.classList.add('active');
            state.memorySelectedItem = item;
            renderLongTermPreview(item);
          });
          list.appendChild(card);
        }
      }
    }
    function renderLongTermPreview(item) {
      const preview = $('memoryLtmPreview');
      const deleteBtn = $('memoryLtmDelete');
      if (!preview) return;
      if (!item) {
        preview.textContent = t('memoryLtmSelectHint');
        preview.scrollTop = 0;
        if (deleteBtn) deleteBtn.style.display = 'none';
        return;
      }
      preview.textContent = (item.fullContent || item.content || '') + '\n\n---\n' + t('memoryDateLabel') + ': ' + (item.id || 'N/A') + '\n' + t('memoryModifiedAtLabel') + ': ' + (item.timestamp || 'N/A');
      preview.scrollTop = 0;
      if (deleteBtn) deleteBtn.style.display = '';
    }
    async function deleteLongTermMemory() {
      if (!state.memorySelectedItem || !state.memorySelectedItem.id) return;
      if (!confirm(t('memoryConfirmDelete'))) return;
      await api('/api/memory/long-term?agent=' + encodeURIComponent(state.agent) + '&id=' + encodeURIComponent(state.memorySelectedItem.id), { method: 'DELETE' });
      state.memorySelectedItem = null;
      state.memoryVectorAtlas = null;
      renderLongTermPreview(null);
      await loadLongTermMemory();
      if (state.memoryTab === 'vector') await loadVectorMemory();
    }

    async function loadVectorMemory() {
      if (!state.agent) return;
      setMemoryState(t('memoryVectorLoading'));
      const data = await api('/api/memory/vector?agent=' + encodeURIComponent(state.agent) + '&maxNodes=240');
      state.memoryVectorAtlas = data;
      renderVectorMemoryMeta();
      renderVectorSearchResults();
      renderVectorAtlas();
      setMemoryState('');
    }

    async function searchVectorMemory() {
      if (!state.agent) return;
      const query = ($('memoryVectorQuery')?.value || '').trim();
      if (!query) {
        state.memoryVectorResults = null;
        state.memoryVectorPinned = null;
        renderVectorSearchResults();
        renderVectorAtlas();
        return;
      }

      setMemoryState(t('memoryVectorSearchLoading'));
      const params = new URLSearchParams({ agent: state.agent, query, take: '8' });
      const data = await api('/api/memory/vector/search?' + params.toString());
      state.memoryVectorResults = data;
      state.memoryVectorPinned = null;
      renderVectorSearchResults();
      renderVectorAtlas();
      setMemoryState('');
    }

    function vectorKindColor(kind) {
      if (kind === 'core') return '#67f7b1';
      if (kind === 'hot') return '#64dbff';
      if (kind === 'long_term') return '#ffd166';
      return '#a78bfa';
    }

    function vectorRgba(hex, alpha) {
      const value = String(hex || '').replace('#', '');
      if (value.length !== 6) return 'rgba(255,255,255,' + alpha + ')';
      const r = parseInt(value.slice(0, 2), 16);
      const g = parseInt(value.slice(2, 4), 16);
      const b = parseInt(value.slice(4, 6), 16);
      return 'rgba(' + r + ',' + g + ',' + b + ',' + alpha + ')';
    }

    function vectorKindLabel(kind) {
      if (kind === 'core') return 'core';
      if (kind === 'hot') return 'hot';
      if (kind === 'long_term') return 'long-term';
      return kind || 'memory';
    }

    function vectorHighlightIds() {
      const ids = new Set();
      const items = state.memoryVectorResults?.items || [];
      for (const item of items) if (item.id) ids.add(item.id);
      if (state.memoryVectorPinned) ids.add(state.memoryVectorPinned);
      if (state.memoryVectorHover?.id) ids.add(state.memoryVectorHover.id);
      return ids;
    }

    function renderVectorMemoryMeta() {
      const atlasMeta = $('memoryVectorMeta');
      if (atlasMeta) {
        const atlas = state.memoryVectorAtlas;
        if (!atlas) {
          atlasMeta.innerHTML = '<span>' + escapeHtml(t('memoryVectorLoading')) + '</span>';
        } else if (!atlas.nodeCount) {
          atlasMeta.innerHTML = '<span>' + escapeHtml(t('memoryVectorAtlasEmpty')) + '</span>';
        } else {
          const updated = atlas.updatedAt ? new Date(atlas.updatedAt).toLocaleString() : 'N/A';
          atlasMeta.innerHTML = [
            '<span>' + escapeHtml(String(atlas.nodeCount || 0)) + ' ' + escapeHtml(t('memoryVectorNodes')) + '</span>',
            '<span>' + escapeHtml(String(atlas.entryCount || 0)) + ' ' + escapeHtml(t('memoryVectorEntries')) + '</span>',
            '<span>' + escapeHtml(atlas.algorithm || 'local') + '</span>',
            '<span>' + escapeHtml(t('memoryVectorUpdated')) + ' ' + escapeHtml(updated) + '</span>'
          ].join('');
        }
      }

      const searchMeta = $('memoryVectorSearchMeta');
      if (searchMeta) {
        const data = state.memoryVectorResults;
        if (!data) {
          searchMeta.innerHTML = '';
        } else {
          searchMeta.innerHTML = [
            '<span>' + escapeHtml(String(data.candidateCount || 0)) + ' ' + escapeHtml(t('memoryVectorCandidates')) + '</span>',
            '<span>' + escapeHtml(String(data.visitedNodes || 0)) + ' ' + escapeHtml(t('memoryVectorVisited')) + '</span>'
          ].join('');
        }
      }
    }

    function renderVectorSearchResults() {
      const list = $('memoryVectorResults');
      if (!list) return;
      renderVectorMemoryMeta();
      list.innerHTML = '';
      const items = state.memoryVectorResults?.items || [];
      if (!state.memoryVectorResults) {
        const empty = document.createElement('div');
        empty.className = 'memory-vector-empty';
        empty.id = 'memoryVectorEmpty';
        empty.textContent = t('memoryVectorEmpty');
        list.appendChild(empty);
        return;
      }
      if (!items.length) {
        const empty = document.createElement('div');
        empty.className = 'memory-vector-empty';
        empty.textContent = t('memoryVectorNoResults');
        list.appendChild(empty);
        return;
      }

      for (const item of items) {
        const card = document.createElement('div');
        card.className = 'memory-vector-result' + (state.memoryVectorPinned === item.id ? ' active' : '');
        card.dataset.vectorResultId = item.id || '';
        const title = document.createElement('div');
        title.className = 'memory-vector-result-title';
        const strong = document.createElement('strong');
        strong.textContent = item.title || item.sourcePath || item.id || 'memory';
        const score = document.createElement('span');
        score.className = 'memory-vector-score';
        score.textContent = t('memoryVectorScore') + ' ' + Number(item.score || 0).toFixed(3);
        title.append(strong, score);

        const source = document.createElement('div');
        source.className = 'memory-vector-source';
        source.textContent = vectorKindLabel(item.kind) + ' - ' + (item.sourcePath || '') + ':' + (item.startLine || 1);

        const preview = document.createElement('div');
        preview.className = 'memory-vector-preview';
        preview.textContent = item.textPreview || '';

        const terms = document.createElement('div');
        terms.className = 'memory-vector-terms';
        for (const term of item.terms || []) {
          const chip = document.createElement('span');
          chip.textContent = term;
          terms.appendChild(chip);
        }

        card.append(title, source, preview, terms);
        card.addEventListener('click', function() {
          state.memoryVectorPinned = item.id || null;
          renderVectorSearchResults();
          renderVectorAtlas();
        });
        list.appendChild(card);
      }
    }

    function renderVectorAtlas() {
      const canvas = $('memoryVectorCanvas');
      if (!canvas) return;
      const rect = canvas.getBoundingClientRect();
      const width = Math.max(1, Math.floor(rect.width));
      const height = Math.max(1, Math.floor(rect.height || 360));
      if (width <= 1 || height <= 1) return;

      const dpr = Math.max(1, Math.min(window.devicePixelRatio || 1, 2));
      if (canvas.width !== Math.floor(width * dpr) || canvas.height !== Math.floor(height * dpr)) {
        canvas.width = Math.floor(width * dpr);
        canvas.height = Math.floor(height * dpr);
      }

      const ctx = canvas.getContext('2d');
      if (!ctx) return;
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
      ctx.clearRect(0, 0, width, height);

      const gradient = ctx.createLinearGradient(0, 0, width, height);
      gradient.addColorStop(0, 'rgba(103,247,177,.045)');
      gradient.addColorStop(1, 'rgba(100,219,255,.035)');
      ctx.fillStyle = gradient;
      ctx.fillRect(0, 0, width, height);

      ctx.save();
      ctx.strokeStyle = 'rgba(255,255,255,.045)';
      ctx.lineWidth = 1;
      for (let x = 0; x <= width; x += 54) {
        ctx.beginPath();
        ctx.moveTo(x, 0);
        ctx.lineTo(x, height);
        ctx.stroke();
      }
      for (let y = 0; y <= height; y += 54) {
        ctx.beginPath();
        ctx.moveTo(0, y);
        ctx.lineTo(width, y);
        ctx.stroke();
      }
      ctx.restore();

      const atlas = state.memoryVectorAtlas;
      const nodes = atlas?.nodes || [];
      const links = atlas?.links || [];
      if (!nodes.length) {
        ctx.fillStyle = 'rgba(238,245,255,.62)';
        ctx.font = '12px system-ui, sans-serif';
        ctx.textAlign = 'center';
        ctx.fillText(t('memoryVectorAtlasEmpty'), width / 2, height / 2);
        state.memoryVectorScreenNodes = [];
        renderVectorTooltip(null);
        renderVectorMemoryMeta();
        return;
      }

      const pad = 28;
      const screenNodes = nodes.map(function(node) {
        return {
          ...node,
          sx: pad + Number(node.x || 0.5) * Math.max(1, width - pad * 2),
          sy: pad + Number(node.y || 0.5) * Math.max(1, height - pad * 2),
          radius: 4.5 + Number(node.size || 0.7) * 3.5
        };
      });
      state.memoryVectorScreenNodes = screenNodes;
      const byId = new Map(screenNodes.map(node => [node.id, node]));
      const highlights = vectorHighlightIds();

      for (const link of links) {
        const a = byId.get(link.sourceId);
        const b = byId.get(link.targetId);
        if (!a || !b) continue;
        const hot = highlights.has(a.id) || highlights.has(b.id);
        ctx.beginPath();
        ctx.moveTo(a.sx, a.sy);
        ctx.lineTo(b.sx, b.sy);
        ctx.strokeStyle = hot ? 'rgba(103,247,177,.38)' : 'rgba(170,184,208,.10)';
        ctx.lineWidth = hot ? 1.4 : Math.max(0.5, Number(link.strength || 0.4) * 1.1);
        ctx.stroke();
      }

      for (const node of screenNodes) {
        const color = vectorKindColor(node.kind);
        const hot = highlights.has(node.id);
        if (hot) {
          const halo = ctx.createRadialGradient(node.sx, node.sy, node.radius, node.sx, node.sy, node.radius * 4.2);
          halo.addColorStop(0, vectorRgba(color, .28));
          halo.addColorStop(1, 'rgba(255,255,255,0)');
          ctx.fillStyle = halo;
          ctx.beginPath();
          ctx.arc(node.sx, node.sy, node.radius * 4.2, 0, Math.PI * 2);
          ctx.fill();
        }

        ctx.beginPath();
        ctx.arc(node.sx, node.sy, node.radius, 0, Math.PI * 2);
        ctx.fillStyle = color;
        ctx.shadowColor = color;
        ctx.shadowBlur = hot ? 18 : 7;
        ctx.fill();
        ctx.shadowBlur = 0;
        ctx.lineWidth = hot ? 2 : 1;
        ctx.strokeStyle = hot ? 'rgba(255,255,255,.82)' : 'rgba(255,255,255,.42)';
        ctx.stroke();
      }

      const labelNode = state.memoryVectorHover || (state.memoryVectorPinned ? byId.get(state.memoryVectorPinned) : null);
      if (labelNode) {
        ctx.font = '11px system-ui, sans-serif';
        ctx.fillStyle = 'rgba(238,245,255,.88)';
        ctx.textAlign = labelNode.sx > width * 0.68 ? 'right' : 'left';
        const x = labelNode.sx + (ctx.textAlign === 'right' ? -12 : 12);
        const y = Math.max(18, labelNode.sy - 12);
        ctx.fillText(labelNode.title || labelNode.sourcePath || labelNode.id, x, y);
      }

      renderVectorTooltip(labelNode);
      renderVectorMemoryMeta();
    }

    function renderVectorTooltip(node) {
      const tooltip = $('memoryVectorTooltip');
      if (!tooltip) return;
      if (!node) {
        tooltip.classList.remove('active');
        tooltip.innerHTML = '';
        return;
      }

      tooltip.innerHTML = '<strong>' + escapeHtml(node.title || node.sourcePath || node.id) + '</strong>'
        + '<div>' + escapeHtml(vectorKindLabel(node.kind)) + ' - ' + escapeHtml(node.sourcePath || '') + ':' + escapeHtml(String(node.startLine || 1)) + '</div>'
        + '<div>' + escapeHtml(node.textPreview || '') + '</div>';
      tooltip.classList.add('active');
    }

    function handleVectorCanvasMove(event) {
      const canvas = $('memoryVectorCanvas');
      if (!canvas || !state.memoryVectorScreenNodes?.length) return;
      const rect = canvas.getBoundingClientRect();
      const x = event.clientX - rect.left;
      const y = event.clientY - rect.top;
      let nearest = null;
      let nearestDistance = Infinity;
      for (const node of state.memoryVectorScreenNodes) {
        const distance = Math.hypot(node.sx - x, node.sy - y);
        if (distance < nearestDistance) {
          nearest = node;
          nearestDistance = distance;
        }
      }
      const next = nearest && nearestDistance <= Math.max(18, nearest.radius * 2.4) ? nearest : null;
      if ((state.memoryVectorHover?.id || null) !== (next?.id || null)) {
        state.memoryVectorHover = next;
        renderVectorAtlas();
      }
    }

    function handleVectorCanvasClick() {
      if (!state.memoryVectorHover?.id) return;
      state.memoryVectorPinned = state.memoryVectorHover.id;
      renderVectorSearchResults();
      renderVectorAtlas();
      let card = null;
      document.querySelectorAll('[data-vector-result-id]').forEach(function(node) {
        if (node.dataset.vectorResultId === state.memoryVectorPinned) card = node;
      });
      if (card) card.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
    }

    /* ===== Organize Progress ===== */
    function showOrganizeProgress(percent, stage, title, desc) {
      const overlay = $('organizeOverlay');
      if (overlay) overlay.classList.add('active');
      if (title && $('organizeTitle')) $('organizeTitle').textContent = title;
      if (desc && $('organizeDesc')) $('organizeDesc').textContent = desc;
      updateOrganizeProgress(percent, stage);
    }
    function updateOrganizeProgress(percent, stage, error) {
      const bar = $('organizeProgressBar');
      const pct = $('organizePercent');
      const stg = $('organizeStage');
      const err = $('organizeError');
      if (bar) bar.style.width = Math.max(0, Math.min(100, percent)) + '%';
      if (pct) pct.textContent = Math.max(0, Math.min(100, percent)) + '%';
      if (stg) stg.textContent = stage || '';
      if (err) err.textContent = error || '';
    }
    function hideOrganizeProgress() {
      const overlay = $('organizeOverlay');
      if (overlay) overlay.classList.remove('active');
    }
    async function pollOrganizationStatus(jobId) {
      const pollInterval = setInterval(async function() {
        try {
          const data = await api('/api/memory/organize/status?jobId=' + encodeURIComponent(jobId));
          updateOrganizeProgress(data.progress, data.stage, data.error);
          if (data.status === 'completed') {
            clearInterval(pollInterval);
            state.memoryOrganizing = false;
            setMemoryState(data.resultSummary === 'no_pending_changes' ? t('memoryOrganizeNoChanges') : t('memoryOrganizeComplete'));
            hideOrganizeProgress();
            await loadMemory();
          } else if (data.status === 'failed') {
            clearInterval(pollInterval);
            state.memoryOrganizing = false;
            setMemoryState(t('memoryOrganizeFailedPrefix') + ': ' + (data.error || t('unknownError')));
            hideOrganizeProgress();
          }
        } catch (err) {
          clearInterval(pollInterval);
          state.memoryOrganizing = false;
          setMemoryState(t('memoryOrganizePollFailedPrefix') + ': ' + err.message);
          hideOrganizeProgress();
        }
      }, 2000);
    }

    async function startSkillsOrganization() {
      if (state.skillsWorking) return;
      try {
        state.skillsWorking = true;
        setSkillsState(t('skillsOrganizeChecking'));
        showOrganizeProgress(0, t('skillsJobPrepare'), t('skillsOrganizeTitle'), t('skillsOrganizeDesc'));
        const res = await api('/api/skills/organize', {
          method: 'POST',
          headers: { 'content-type': 'application/json' },
          body: JSON.stringify({ agent: state.agent })
        });
        if (res.status === 'busy') {
          setSkillsState(t('skillsOrganizeBusy'));
          hideOrganizeProgress();
          state.skillsWorking = false;
          return;
        }
        if (res.status === 'started' && res.jobId) {
          setSkillsState(t('skillsOrganizeStarted'));
          pollSkillsJobStatus(res.jobId, 'organize');
        }
      } catch (err) {
        setSkillsState(t('skillsJobStartFailedPrefix') + ': ' + err.message);
        hideOrganizeProgress();
        state.skillsWorking = false;
      }
    }

    function skillsLearnFileName(file) {
      return file?.webkitRelativePath || file?.name || 'source';
    }

    function updateSkillsLearnFileSummary() {
      const summary = $('skillsLearnSelected');
      if (!summary) return;
      const files = state.skillsLearnFiles || [];
      if (!files.length) {
        summary.textContent = t('skillsLearnNoFiles');
        return;
      }

      const totalSize = files.reduce(function(total, file) { return total + (file.size || 0); }, 0);
      const selectedText = t('skillsLearnSelectedFiles')
        .replace('{count}', String(files.length))
        .replace('{size}', formatBytes(totalSize));
      const visibleNames = files.slice(0, 3).map(skillsLearnFileName).join(', ');
      const moreCount = files.length - 3;
      const moreText = moreCount > 0 ? ', ' + t('skillsLearnMoreFiles').replace('{count}', String(moreCount)) : '';
      summary.textContent = selectedText + ' · ' + visibleNames + moreText;
    }

    function addSkillsLearnFiles(fileList) {
      const files = Array.from(fileList || []);
      if (!files.length) return;
      const seen = new Set((state.skillsLearnFiles || []).map(function(file) {
        return skillsLearnFileName(file) + '|' + file.size + '|' + file.lastModified;
      }));
      const next = (state.skillsLearnFiles || []).slice();
      for (const file of files) {
        const key = skillsLearnFileName(file) + '|' + file.size + '|' + file.lastModified;
        if (seen.has(key)) continue;
        seen.add(key);
        next.push(file);
      }
      state.skillsLearnFiles = next;
      updateSkillsLearnFileSummary();
    }

    function clearSkillsLearnFiles() {
      state.skillsLearnFiles = [];
      if ($('skillsLearnFileInput')) $('skillsLearnFileInput').value = '';
      if ($('skillsLearnFolderInput')) $('skillsLearnFolderInput').value = '';
      updateSkillsLearnFileSummary();
    }

    function showSkillsLearnDialog() {
      const overlay = $('skillsLearnOverlay');
      if (!overlay) return;
      updateSkillsLang();
      overlay.classList.add('active');
      overlay.setAttribute('aria-hidden', 'false');
      updateSkillsLearnFileSummary();
      $('skillsLearnNameHint')?.focus();
    }

    function hideSkillsLearnDialog() {
      const overlay = $('skillsLearnOverlay');
      if (!overlay) return;
      overlay.classList.remove('active');
      overlay.setAttribute('aria-hidden', 'true');
    }

    function handleSkillsLearnOverlayClick(event) {
      const target = event.target;
      if (!(target instanceof Element)) return;

      if (target === $('skillsLearnOverlay')) {
        hideSkillsLearnDialog();
        return;
      }

      const button = target.closest('#skillsLearnCancel,#skillsLearnClose,#skillsLearnChooseFiles,#skillsLearnChooseFolder,#skillsLearnClearFiles,#skillsLearnStart');
      if (!button) return;
      event.preventDefault();

      if (button.id === 'skillsLearnCancel' || button.id === 'skillsLearnClose') {
        hideSkillsLearnDialog();
      } else if (button.id === 'skillsLearnChooseFiles') {
        $('skillsLearnFileInput')?.click();
      } else if (button.id === 'skillsLearnChooseFolder') {
        $('skillsLearnFolderInput')?.click();
      } else if (button.id === 'skillsLearnClearFiles') {
        clearSkillsLearnFiles();
      } else if (button.id === 'skillsLearnStart') {
        startSkillLearningValidation();
      }
    }

    function handleSkillsLearnOverlayChange(event) {
      const target = event.target;
      if (!(target instanceof HTMLInputElement)) return;
      if (target.id === 'skillsLearnFileInput' || target.id === 'skillsLearnFolderInput') {
        addSkillsLearnFiles(target.files);
      }
    }

    async function startSkillLearningValidation() {
      if (state.skillsWorking) return;
      const sourcePath = $('skillsLearnPath')?.value.trim() || '';
      const sourceText = $('skillsLearnText')?.value.trim() || '';
      const nameHint = $('skillsLearnNameHint')?.value.trim() || '';
      const sourceFiles = state.skillsLearnFiles || [];
      if (!sourcePath && !sourceText && sourceFiles.length === 0) {
        setSkillsState(t('skillsLearnNeedSource'));
        return;
      }
      try {
        state.skillsWorking = true;
        hideSkillsLearnDialog();
        setSkillsState(t('skillsLearnStarted'));
        showOrganizeProgress(0, t('skillsJobPrepare'), t('skillsLearnTitleProgress'), t('skillsLearnDescProgress'));
        let requestOptions;
        if (sourceFiles.length > 0) {
          const form = new FormData();
          form.append('agent', state.agent || '');
          form.append('sourcePath', sourcePath);
          form.append('sourceText', sourceText);
          form.append('nameHint', nameHint);
          for (const file of sourceFiles) {
            form.append('sources', file, skillsLearnFileName(file));
          }
          requestOptions = { method: 'POST', body: form };
        } else {
          requestOptions = {
            method: 'POST',
            headers: { 'content-type': 'application/json' },
            body: JSON.stringify({ agent: state.agent, sourcePath, sourceText, nameHint })
          };
        }
        const res = await api('/api/skills/learn-validate', requestOptions);
        if (res.status === 'busy') {
          setSkillsState(t('skillsOrganizeBusy'));
          hideOrganizeProgress();
          state.skillsWorking = false;
          return;
        }
        if (res.status === 'started' && res.jobId) {
          pollSkillsJobStatus(res.jobId, 'learn_validate');
        }
      } catch (err) {
        setSkillsState(t('skillsJobStartFailedPrefix') + ': ' + err.message);
        hideOrganizeProgress();
        state.skillsWorking = false;
      }
    }

    async function startSkillValidation(skillId) {
      if (state.skillsWorking) return;
      try {
        state.skillsWorking = true;
        setSkillsState(t('skillsValidateStarted'));
        showOrganizeProgress(0, t('skillsJobPrepare'), t('skillsValidateTitle'), t('skillsValidateDesc'));
        const res = await api('/api/skills/validate', {
          method: 'POST',
          headers: { 'content-type': 'application/json' },
          body: JSON.stringify({ agent: state.agent, skillId })
        });
        if (res.status === 'busy') {
          setSkillsState(t('skillsOrganizeBusy'));
          hideOrganizeProgress();
          state.skillsWorking = false;
          return;
        }
        if (res.status === 'started' && res.jobId) {
          pollSkillsJobStatus(res.jobId, 'validate', skillId);
        }
      } catch (err) {
        setSkillsState(t('skillsJobStartFailedPrefix') + ': ' + err.message);
        hideOrganizeProgress();
        state.skillsWorking = false;
      }
    }

    async function pollSkillsJobStatus(jobId, kind, skillId) {
      const pollInterval = setInterval(async function() {
        try {
          const data = await api('/api/skills/job/status?jobId=' + encodeURIComponent(jobId));
          updateOrganizeProgress(data.progress, data.stage, data.error);
          if (data.status === 'completed') {
            clearInterval(pollInterval);
            state.skillsWorking = false;
            const summary = data.resultSummary ? ': ' + data.resultSummary : '';
            if (kind === 'organize') {
              setSkillsState(t('skillsOrganizeComplete') + summary);
              hideOrganizeProgress();
              await loadSkills();
            } else if (kind === 'learn_validate') {
              setSkillsState(t('skillsLearnComplete') + summary);
              hideOrganizeProgress();
              await loadSkills();
              const learnedSkillId = data.skillId || skillId;
              if (learnedSkillId) await readSkill(learnedSkillId);
              else if (data.report) renderSkillValidationReport(data.report);
            } else {
              setSkillsState(t('skillsValidateComplete') + summary);
              hideOrganizeProgress();
              if (data.report) renderSkillValidationReport(data.report);
              if (skillId) state.selectedSkillId = skillId;
              if (state.skills) renderSkillsList(state.skills);
            }
          } else if (data.status === 'failed') {
            clearInterval(pollInterval);
            state.skillsWorking = false;
            setSkillsState(t('skillsJobFailedPrefix') + ': ' + (data.error || t('unknownError')));
            hideOrganizeProgress();
          }
        } catch (err) {
          clearInterval(pollInterval);
          state.skillsWorking = false;
          setSkillsState(t('skillsJobPollFailedPrefix') + ': ' + err.message);
          hideOrganizeProgress();
        }
      }, 2000);
    }

    function renderSkillValidationReport(report) {
      const preview = $('skillPreview');
      if (!preview) return;
      state.selectedSkillValidationReport = report || null;
      preview.innerHTML = '<div style="white-space:pre-wrap;">' + escapeHtml(report) + '</div>';
      setNodeText('#skillsPreviewTitle', t('skillsValidationReportTitle'));
    }

    function renderTask(task) {
      const card = $('taskCard');
      if (!card) return;
      card.classList.remove('expanded');
      if (!task) {
        card.innerHTML = '<div class="task-empty">' + escapeHtml(t('noActiveTask')) + '</div>';
        return;
      }
      const steps = task.steps || [];
      const visibleSteps = steps.slice(0, 3);
      const done = steps.filter(step => normalizeTaskStatus(step.status) === 'done').length;
      const taskKind = normalizeTaskStatus(task.status);
      card.innerHTML = '';
      const head = document.createElement('div');
      head.className = 'task-head';
      const title = document.createElement('div');
      title.className = 'task-title';
      const titleRow = document.createElement('div');
      titleRow.className = 'task-title-row';
      const taskIcon = document.createElement('span');
      taskIcon.className = `task-step-icon ${taskKind}`;
      taskIcon.textContent = taskStatusIcon(task.status);
      const strong = document.createElement('strong');
      strong.textContent = task.title || t('activeTask');
      strong.title = task.title || t('activeTask');
      titleRow.append(taskIcon, strong);
      const sub = document.createElement('span');
      sub.textContent = (task.status || 'in_process') + ' · ' + done + '/' + steps.length + ' ' + t('steps');
      title.append(titleRow, sub);
      const toggle = document.createElement('button');
      toggle.type = 'button';
      toggle.className = 'task-toggle';
      toggle.textContent = '▸ ' + t('steps');
      const list = document.createElement('ul');
      list.className = 'task-steps';
      for (const step of visibleSteps) {
        const statusKind = normalizeTaskStatus(step.status);
        const fullText = step.for_what ?? step.forWhat ?? '';
        const item = document.createElement('li');
        item.className = `task-step ${statusKind}`;
        item.title = fullText;
        const icon = document.createElement('span');
        icon.className = 'task-step-icon';
        icon.textContent = taskStatusIcon(step.status);
        const index = document.createElement('b');
        index.textContent = step.index || '•';
        const text = document.createElement('span');
        text.textContent = truncateText(fullText, 10);
        item.append(icon, index, text);
        list.appendChild(item);
      }
      toggle.addEventListener('click', () => {
        card.classList.toggle('expanded');
        toggle.textContent = (card.classList.contains('expanded') ? '▾ ' : '▸ ') + t('steps');
      });
      head.append(title, toggle);
      card.append(head, list);
    }

    function simpleNoticeHash(text) {
      let hash = 0;
      const value = String(text || '');
      for (let i = 0; i < value.length; i++) hash = ((hash << 5) - hash + value.charCodeAt(i)) | 0;
      return String(hash);
    }

    function scheduledNoticeKey(msg) {
      if (!msg || msg.role !== 'assistant' || msg.messageType !== 'scheduled_task_notice') return null;
      const content = String(msg.content || '');
      const runMatch = content.match(/Run ID[：:]\s*([^\s]+)/i);
      return runMatch ? runMatch[1] : simpleNoticeHash(content);
    }

    function imageNoticeKey(msg) {
      if (!msg || msg.role !== 'assistant' || msg.messageType !== 'image_generation_notice') return null;
      const content = String(msg.content || '');
      const jobMatch = content.match(/Job ID\S*\s*([^\s]+)/i);
      return jobMatch ? jobMatch[1] : simpleNoticeHash(content);
    }

    function collectScheduledNoticeKeys(messages) {
      const keys = new Set();
      (messages || []).forEach(function(msg) {
        const key = scheduledNoticeKey(msg);
        if (key) keys.add(key);
      });
      return keys;
    }

    function collectImageNoticeKeys(messages) {
      const keys = new Set();
      (messages || []).forEach(function(msg) {
        const key = imageNoticeKey(msg);
        if (key) keys.add(key);
      });
      return keys;
    }

    function renderMessages(messages) {
      const chat = $('chat');
      if (!chat) return;
      const items = messages || [];
      const previousBottomOffset = chat.scrollHeight - chat.scrollTop;
      const hadRenderableContent = chat.children.length > 0;
      const wasNearBottom = isChatNearBottom(chat);
      state.suppressChatAutoScroll = true;
      try {
        const fragment = document.createDocumentFragment();
        if (!items.length) {
          const node = document.createElement('div');
          node.className = 'empty';
          node.textContent = t('sessionReady');
          fragment.appendChild(node);
        } else {
          for (let i = 0; i < items.length; i++) {
            const msg = items[i];
            if (msg.role === 'user') {
              addMessage('user', msg.content, t('you'), { attachments: msg.attachments || [], target: fragment, scroll: false });
              continue;
            }
            if (msg.role === 'assistant') {
              if (msg.messageType === 'scheduled_task_notice') {
                renderNoticeCard(msg.content, { target: fragment, scroll: false });
                continue;
              }
              if (msg.messageType === 'image_generation_notice') {
                renderImageNoticeCard(msg.content, { target: fragment, scroll: false });
                continue;
              }
              if (msg.reasoningContent) {
                addThinkingCard(msg.reasoningContent, null, { target: fragment, scroll: false });
              }
              const assistantContent = msg.content ? msg.content : (msg.toolCalls?.length ? '(tool call)' : '');
              const card = addMessage('assistant', assistantContent, assistantMeta(), { messageIndex: i, audio: msg.audio, target: fragment, scroll: false });
              const calls = msg.toolCalls || [];
              if (calls.length) {
                const results = new Map();
                let j = i + 1;
                while (j < items.length && items[j].role === 'tool') {
                  results.set(items[j].toolCallId, items[j]);
                  j++;
                }
                const group = createToolGroup(card.card);
                for (const tc of calls) {
                  const result = results.get(tc.id);
                  group.upsert({
                    id: tc.id,
                    name: tc.name,
                    detail: tc.arguments,
                    status: result ? 'done' : 'called',
                    result: result ? result.content : t('toolRequestSent')
                  });
                }
                i = j - 1;
              }
              continue;
            }
            if (msg.role === 'tool') addMessage('tool', summarizeToolValue(msg.content), t('toolResult'), { target: fragment, scroll: false });
          }
        }
        chat.replaceChildren(fragment);
      } finally {
        state.suppressChatAutoScroll = false;
        if (items.length) {
          if (hadRenderableContent && !wasNearBottom) {
            setChatScrollTop(chat, chat.scrollHeight - previousBottomOffset, 180);
          } else {
            setChatScrollTop(chat, chat.scrollHeight, 0);
          }
        }
        state.chatNearBottom = isChatNearBottom(chat);
        if (state.busy) state.chatFollowStream = state.chatNearBottom;
        updateChatJumpButton();
      }
    }
    function localizeNoticeTitle(title) {
      return title === '定时任务通知' || title === 'Scheduled Task Notice' ? t('scheduledNotice') : title;
    }
    function noticeLabelKeyFull(label) {
      const normalized = String(label || '').trim().toLowerCase().replace(/\s+/g, ' ');
      const zh = {
        '\u4efb\u52a1': 'task',
        '\u72b6\u6001': 'status',
        '\u8ba1\u5212\u65f6\u95f4': 'scheduled',
        '\u5b8c\u6210\u65f6\u95f4': 'completed',
        '\u8865\u507f\u539f\u56e0': 'catchup',
        '\u89e6\u53d1': 'trigger'
      };
      if (zh[normalized]) return zh[normalized];
      if (normalized === 'task') return 'task';
      if (normalized === 'status') return 'status';
      if (normalized === 'run id') return 'run';
      if (normalized === 'trigger') return 'trigger';
      if (normalized === 'scheduled at') return 'scheduled';
      if (normalized === 'completed at') return 'completed';
      if (normalized === 'catch-up reason' || normalized === 'catch up reason') return 'catchup';
      return '';
    }
    function parseNoticeMetaLineFull(line) {
      const match = String(line || '').match(/^([^:\uff1a]+)[:\uff1a]\s*(.*)$/);
      if (!match) return null;
      const key = noticeLabelKeyFull(match[1]);
      return key ? { key, raw: line, value: match[2] || '' } : null;
    }
    function parseNoticeContentFull(content) {
      const raw = String(content || '').replace(/\r\n/g, '\n');
      const withoutFooter = raw.replace(/\n---\s*\nThis is a low-priority notice[\s\S]*$/i, '').trimEnd();
      let text = withoutFooter;
      let title = t('scheduledNotice');
      const titleMatch = text.match(/^##\s+(.+?)\s*\n+/);
      if (titleMatch) {
        title = titleMatch[1].trim();
        text = text.slice(titleMatch[0].length);
      }
      const lines = text.split('\n');
      const tags = [];
      let bodyStart = 0;
      let sawMeta = false;
      for (let i = 0; i < lines.length; i++) {
        const line = lines[i];
        if (!line.trim()) {
          bodyStart = i + 1;
          continue;
        }
        const meta = parseNoticeMetaLineFull(line.trim());
        if (!meta) {
          bodyStart = i;
          break;
        }
        sawMeta = true;
        tags.push(meta);
        bodyStart = i + 1;
      }
      return { title, tags, body: lines.slice(bodyStart).join('\n').trim() };
    }
    function localizeNoticeTagFull(tag) {
      const item = typeof tag === 'string' ? parseNoticeMetaLineFull(tag) : tag;
      if (!item) return String(tag || '');
      let label = item.key;
      let value = item.value || '';
      if (item.key === 'task') label = t('noticeTaskLabel');
      else if (item.key === 'status') {
        label = t('noticeStatusLabel');
        value = formatRunStatus(value);
      }
      else if (item.key === 'run') label = t('noticeRunIdLabel');
      else if (item.key === 'trigger') {
        label = t('noticeTriggerLabel');
        if (String(value).trim().toLowerCase() === 'manual test') value = t('scheduleManualTestLabel');
      }
      else if (item.key === 'scheduled') label = t('noticeScheduledAtLabel');
      else if (item.key === 'completed') label = t('noticeCompletedAtLabel');
      else if (item.key === 'catchup') label = t('noticeCatchUpLabel');
      return label + ': ' + value;
    }
    function parseImageNoticeContent(content) {
      const data = { fields: {}, files: [], batchFiles: [] };
      let section = '';
      String(content || '').split('\n').map(line => line.trim()).filter(Boolean).forEach(function(line) {
        if (line.startsWith('## ')) return;
        if (line.startsWith('{show_file:')) return;
        if (line === 'This notice is authoritative host state for this image job.') return;
        if (line === 'Generated files:') { section = 'files'; return; }
        if (line === 'Batch generated files:') { section = 'batchFiles'; return; }
        if (line.startsWith('- ')) {
          const text = line.slice(2).trim();
          const path = text.replace(/\s*\(.*/, '').trim();
          const promptMatch = text.match(/prompt:\s*([^)]+)/i);
          const jobMatch = text.match(/job:\s*([^,)]+)/i);
          const item = { path, prompt: promptMatch ? promptMatch[1].trim() : '', job: jobMatch ? jobMatch[1].trim() : '' };
          if (section === 'batchFiles') data.batchFiles.push(item);
          else data.files.push(item);
          return;
        }
        const match = line.match(/^([^:]+):\s*(.*)$/);
        if (match) data.fields[match[1].trim().toLowerCase()] = match[2].trim();
      });
      return data;
    }
    function localizeImageStatus(value) {
      const key = String(value || '').trim().toLowerCase();
      const map = { queued:'imageStatusQueued', running:'imageStatusRunning', succeeded:'imageStatusSucceeded', failed:'imageStatusFailed', canceled:'imageStatusCanceled', cancelled:'imageStatusCanceled', complete:'imageStatusComplete', active:'imageStatusActive' };
      return map[key] ? t(map[key]) : value;
    }
    function localizeImageBatchStatus(value) {
      const raw = String(value || '');
      const match = raw.match(/^(complete|active)\s*\((\d+)\s+succeeded,\s*(\d+)\s+failed,\s*(\d+)\s+canceled\)/i);
      if (!match) return raw;
      return `${localizeImageStatus(match[1])} (${t('imageBatchSucceeded')}: ${match[2]}, ${t('imageBatchFailed')}: ${match[3]}, ${t('imageBatchCanceled')}: ${match[4]})`;
    }
    function imageNoticeField(labelKey, value) {
      if (!value) return null;
      const chip = document.createElement('span');
      chip.className = 'schedule-rule-chip';
      chip.style.cssText = 'font-size:11px;padding:3px 8px;border-color:rgba(100,219,255,.24);background:rgba(100,219,255,.08);color:#dff8ff;';
      chip.textContent = t(labelKey) + ': ' + value;
      return chip;
    }
    function renderImageNoticeCard(content, options = {}) {
      const target = options.target || $('chat');
      if (!target) return;
      const parsed = parseImageNoticeContent(content);
      const fields = parsed.fields;
      const wrap = document.createElement('div');
      wrap.className = 'message assistant';
      const avatar = document.createElement('div');
      avatar.className = 'avatar';
      applyAvatar(avatar, 'assistant');
      const stack = document.createElement('div');
      stack.className = 'stack';
      const meta = document.createElement('div');
      meta.className = 'meta';
      meta.textContent = t('imageNoticeMeta');
      const card = document.createElement('div');
      card.className = 'notice-card';
      const badge = document.createElement('span');
      badge.className = 'notice-badge';
      badge.textContent = t('imageNoticeBadge');
      const head = document.createElement('div');
      head.className = 'notice-head';
      head.appendChild(badge);
      const tagsRow = document.createElement('div');
      tagsRow.style.cssText = 'display:flex;flex-wrap:wrap;gap:6px;margin-top:8px;';
      [
        imageNoticeField('imageNoticeJobId', fields['job id']),
        imageNoticeField('imageNoticeBatchId', fields['batch id']),
        imageNoticeField('imageNoticeStatus', localizeImageStatus(fields.status)),
        imageNoticeField('imageNoticeBatchStatus', localizeImageBatchStatus(fields['batch status'])),
        imageNoticeField('imageNoticeFallback', fields['provider fallback'] ? t('yes') : ''),
        imageNoticeField('imageNoticeProvider', fields['final provider/model'])
      ].forEach(function(node) { if (node) tagsRow.appendChild(node); });
      if (tagsRow.children.length) head.appendChild(tagsRow);
      const titleEl = document.createElement('div');
      titleEl.className = 'notice-title';
      titleEl.textContent = t('imageNoticeTitle');
      const bodyEl = document.createElement('div');
      bodyEl.className = 'notice-body markdown';
      const lines = [];
      if (fields.prompt) lines.push(`**${t('imageNoticePrompt')}**: ${fields.prompt}`);
      if (fields['requested profile']) lines.push(`**${t('imageNoticeRequestedProfile')}**: ${fields['requested profile']}`);
      if (fields.error) lines.push(`**${t('imageNoticeError')}**: ${fields.error}`);
      if (fields['error category']) lines.push(`**${t('imageNoticeErrorCategory')}**: ${fields['error category']}`);
      const files = parsed.batchFiles.length ? parsed.batchFiles : parsed.files;
      if (files.length) {
        lines.push('');
        lines.push(`**${parsed.batchFiles.length ? t('imageNoticeBatchFiles') : t('imageNoticeFiles')}**`);
        files.forEach(function(file) {
          const parts = [file.path];
          if (file.job) parts.push(`${t('imageNoticeJobId')}: ${file.job}`);
          if (file.prompt) parts.push(`${t('imageNoticePrompt')}: ${file.prompt}`);
          lines.push('- ' + parts.join(' | '));
        });
      }
      lines.push('');
      lines.push(t('imageNoticeAuthority'));
      setMarkdownContent(bodyEl, lines.join('\n'));
      if (isPreviewFileContent(content)) {
        const previewContainer = renderPreviewFiles(content);
        if (previewContainer) {
          previewContainer.style.marginTop = '12px';
          bodyEl.appendChild(previewContainer);
        }
      }
      const time = document.createElement('div');
      time.className = 'notice-time';
      time.textContent = new Date().toLocaleString(state.lang === 'zh' ? 'zh-CN' : 'en-US');
      card.append(head, titleEl, bodyEl, time);
      stack.append(meta, card);
      wrap.append(avatar, stack);
      target.appendChild(wrap);
      if (options.scroll !== false) scrollBottom();
    }
    function renderNoticeCard(content, options = {}) {
      const target = options.target || $('chat');
      if (!target) return;
      const parsed = parseNoticeContentFull(content);
      const wrap = document.createElement('div');
      wrap.className = 'message assistant';
      const avatar = document.createElement('div');
      avatar.className = 'avatar';
      applyAvatar(avatar, 'assistant');
      const stack = document.createElement('div');
      stack.className = 'stack';
      const meta = document.createElement('div');
      meta.className = 'meta';
      meta.textContent = t('scheduledNotice');
      const card = document.createElement('div');
      card.className = 'notice-card';
      const badge = document.createElement('span');
      badge.className = 'notice-badge';
      badge.textContent = t('scheduledNotice');
      const head = document.createElement('div');
      head.className = 'notice-head';
      head.appendChild(badge);
      if (parsed.tags.length) {
        const tagsRow = document.createElement('div');
        tagsRow.style.cssText = 'display:flex;flex-wrap:wrap;gap:6px;margin-top:8px;';
        parsed.tags.forEach(function(tag) {
          const chip = document.createElement('span');
          chip.className = 'schedule-rule-chip';
          chip.style.cssText = 'font-size:11px;padding:3px 8px;border-color:rgba(103,247,177,.22);background:rgba(103,247,177,.08);color:#d9fff0;';
          chip.textContent = localizeNoticeTagFull(tag);
          tagsRow.appendChild(chip);
        });
        head.appendChild(tagsRow);
      }
      const titleEl = document.createElement('div');
      titleEl.className = 'notice-title';
      titleEl.textContent = localizeNoticeTitle(parsed.title);
      const bodyEl = document.createElement('div');
      bodyEl.className = 'notice-body markdown';
      setMarkdownContent(bodyEl, parsed.body);

      // Check for preview_file in notice content and render inline previews
      if (isPreviewFileContent(content)) {
        const previewContainer = renderPreviewFiles(content);
        if (previewContainer) {
          previewContainer.style.marginTop = '12px';
          bodyEl.appendChild(previewContainer);
        }
      }

      const time = document.createElement('div');
      time.className = 'notice-time';
      const completedTag = parsed.tags.find(function(tag) { return tag && tag.key === 'completed'; });
      time.textContent = completedTag ? completedTag.value : new Date().toLocaleString(state.lang === 'zh' ? 'zh-CN' : 'en-US');
      card.append(head, titleEl, bodyEl, time);
      stack.append(meta, card);
      wrap.append(avatar, stack);
      target.appendChild(wrap);
      if (options.scroll !== false) scrollBottom();
    }

    async function triggerHostNoticeContinuation() {
      if (state.hostNoticeContinuationRunning || state.busy || state.sessionReadOnly || !state.agent || !state.session) return;
      state.hostNoticeContinuationRunning = true;
      try {
        await sendMessage('', { continueFromHostNotice: true });
      } finally {
        state.hostNoticeContinuationRunning = false;
      }
    }

    async function sendMessage(text, options = {}) {
      const continueFromHostNotice = options.continueFromHostNotice === true;
      state.chatFollowStream = isChatNearBottom($('chat'), 110);
      const outgoingAttachments = continueFromHostNotice ? [] : (state.chatAttachments || []).slice();
      const visibleText = text || (outgoingAttachments.length ? t('fileAttachment') : '');
      if (!continueFromHostNotice) {
        addMessage('user', visibleText, t('you'), { attachments: outgoingAttachments });
      }
      const ai = addAssistantStreaming();
      const controller = new AbortController();
      state.abortController = controller;
      setBusy(true, 'thinking');
      let thinkingCuePlayed = false;
      const playThinkingCueOnce = function() {
        if (thinkingCuePlayed) return;
        thinkingCuePlayed = true;
        playSoundCue('thinking');
      };
      playThinkingCueOnce();
      let finalAssistantIndex = -1;
      let finalSpeechText = '';
      let finalAudio = null;
      let speechTask = null;
      let thinking = null;
      const streamMarkerCues = new Set();
      const contentMarkerStream = createPlayAudioMarkerStream();
      const orderedUi = createOrderedUiQueue();
      const closeCueSegment = async function(lane) {
        const segment = lane === 'thinking' ? thinking?.currentSoundCueSegment : ai.currentSoundCueSegment;
        if (!segment || segment.closed) return;
        segment.closed = true;
        await wait(soundCueDisplayDelayMs());
        markSoundCueEventPlayed(segment.block);
        if (lane === 'thinking' && thinking?.currentSoundCueSegment === segment) {
          thinking.currentSoundCueSegment = null;
        } else if (lane !== 'thinking' && ai.currentSoundCueSegment === segment) {
          ai.currentSoundCueSegment = null;
        }
      };
      const enqueueStreamPieces = function(pieces, lane) {
        let enqueued = false;
        pieces.forEach(function(piece) {
          if (piece.type === 'text' && piece.text) {
            enqueued = true;
            orderedUi.enqueue(function() {
              if (lane === 'thinking') {
                thinking = appendThinking(thinking, piece.text, ai.wrap);
              } else {
                appendAssistantStreamText(ai, piece.text);
              }
            });
          } else if (piece.type === 'cue' && piece.cue) {
            enqueued = true;
            orderedUi.enqueue(async function() {
              await closeCueSegment(lane);
              streamMarkerCues.add(piece.cue);
              const block = lane === 'thinking'
                ? (function() {
                    const result = appendThinkingSoundCue(thinking, piece.cue, ai.wrap);
                    thinking = result.thinking;
                    return result.block;
                  })()
                : appendAssistantSoundCue(ai, piece.cue);
              await playSoundCue(piece.cue, { waitUntilStarted: true, force: true });
            });
          }
        });
        return enqueued;
      };
      try {
      if (!state.multimodal) await loadMultiModalConfig();

      let requestBody;
      let requestHeaders = undefined;
      if (outgoingAttachments.length) {
        const form = new FormData();
        form.append('agent', state.agent);
        form.append('session', state.session);
        form.append('message', text);
        outgoingAttachments.forEach(function(item) {
          form.append('attachments', item.file, item.name);
        });
        requestBody = form;
      } else {
        requestHeaders = { 'content-type': 'application/json' };
        requestBody = JSON.stringify({ agent: state.agent, session: state.session, message: text, continueFromHostNotice });
      }

      const res = await fetch('/api/chat', {
        method: 'POST',
        headers: requestHeaders,
        body: requestBody,
        signal: controller.signal
      });
      if (!res.ok || !res.body) throw new Error(await res.text());
      if (outgoingAttachments.length) clearChatAttachments(false);

      const reader = res.body.getReader();
      const decoder = new TextDecoder();
      let buffer = '';
      while (true) {
        const { value, done } = await reader.read();
        if (done) break;
        buffer += decoder.decode(value, { stream: true });
        const lines = buffer.split('\n');
        buffer = lines.pop();
        for (const line of lines) {
          if (!line.trim()) continue;
          const evt = JSON.parse(line);
          if (evt.type === 'chunk') {
            ai.buffer += evt.text;
            enqueueStreamPieces(consumePlayAudioMarkerStream(contentMarkerStream, evt.text), 'reply');
          } else if (evt.type === 'thinking') {
            orderedUi.enqueue(function() {
              thinking = appendThinking(thinking, evt.text, ai.wrap);
            });
          } else if (evt.type === 'audio_cue') {
            // Marker playback is driven by the streamed text parser so the cue
            // can stay visually ordered with the exact message segment.
          } else if (evt.type === 'phase') {
            setBusy(true, evt.phase);
            if (String(evt.phase || '').toLowerCase() === 'thinking') playThinkingCueOnce();
          } else if (evt.type === 'tool_start') {
            orderedUi.enqueue(async function() {
              await closeCueSegment('thinking');
              finishThinking(thinking);
              ai.toolGroup.upsert({ id: evt.id, name: evt.name, detail: evt.detail, status: 'running', result: t('waitingOutput') });
            });
          } else if (evt.type === 'tool_result') {
            ai.toolGroup.upsert({ id: evt.id, name: evt.name, detail: evt.detail, status: evt.status, result: evt.result });
          } else if (evt.type === 'stats') {
            updateStats(evt.session);
            renderTask(evt.activeTask);
          } else if (evt.type === 'error') {
            ai.buffer += `\n\n[error] ${evt.message}`;
            orderedUi.enqueue(function() {
              appendAssistantStreamText(ai, `\n\n[error] ${evt.message}`);
            });
          } else if (evt.type === 'audio_error') {
            finalAudio = null;
          } else if (evt.type === 'done') {
            updateStats(evt.session);
            if (typeof evt.session?.totalMessages === 'number') state.lastMessageCount = evt.session.totalMessages;
            renderTask(evt.activeTask);
            finalAssistantIndex = typeof evt.lastAssistantIndex === 'number' ? evt.lastAssistantIndex : -1;
            finalSpeechText = typeof evt.speechText === 'string' ? evt.speechText : '';
            finalAudio = evt.audio || null;
            speechTask = prepareSpeechAfterStream(ai, finalSpeechText || ai.buffer, finalAssistantIndex, finalAudio);
          }
        }
      }
      orderedUi.enqueue(async function() {
        await closeCueSegment('thinking');
        await closeCueSegment('reply');
      });
      await orderedUi.drain();
      stopMatrix();
      finishThinking(thinking);
      // Final render with preview parsing after streaming is complete
      finalizeAssistantTextSegments(ai);
      const markerCues = new Set(streamMarkerCues);
      extractPlayAudioMarkers(ai.buffer).forEach(type => markerCues.add(type));
      if (!stripPlayAudioMarkers(ai.buffer).trim() && !(ai.soundCueCount > 0)) {
        ai.buffer = t('noVisible');
        appendAssistantStreamText(ai, ai.buffer);
        finalizeAssistantTextSegments(ai);
      }
      ai.loader.textContent = '✓';
      ai.meta.textContent = assistantMeta(t('complete'));
      setBusy(false);
      if (!speechTask) speechTask = prepareSpeechAfterStream(ai, finalSpeechText || ai.buffer, finalAssistantIndex, finalAudio);
      if (!markerCues.has('reply_done')) {
        await playSoundCue('reply_done', { waitUntilEnded: true });
      } else {
        await waitForSoundCueQueueIdle();
      }
      await maybeAutoPlayPreparedSpeech(speechTask);
      await loadSession();
      await loadSessions({ reloadCurrentSession: false });
      } catch (err) {
        orderedUi.enqueue(async function() {
          await closeCueSegment('thinking');
          await closeCueSegment('reply');
        });
        await orderedUi.drain();
        stopMatrix();
        finishThinking(thinking);
        let stopped = false;
        if (err) {
          if (err.name === 'AbortError') stopped = true;
        }
        if (stopped) {
          const stoppedText = ai.buffer.trim() ? '\n\n' + t('stoppedMessage') : t('stoppedMessage');
          ai.buffer += stoppedText;
          appendAssistantStreamText(ai, stoppedText);
          finalizeAssistantTextSegments(ai);
          ai.loader.textContent = '■';
          ai.meta.textContent = assistantMeta(t('stopped'));
        } else {
          let message = String(err);
          if (err) {
            if (err.message) message = err.message;
          }
          const errorText = '\n\n[error] ' + message;
          ai.buffer += errorText;
          appendAssistantStreamText(ai, errorText);
          finalizeAssistantTextSegments(ai);
          ai.loader.textContent = '!';
          ai.meta.textContent = assistantMeta('error');
        }
        setBusy(false);
      } finally {
        if (state.abortController === controller) state.abortController = null;
      }
    }

    function showBlankPage(kind) {
      $('blankTitle').textContent = kind === 'close' ? t('blankClosedTitle') : t('blankMinimizedTitle');
      $('blankText').textContent = t('blankActionText');
      document.querySelector('.home-shell').style.display = 'none';
      $('blankPage').classList.add('active');
      $('blankPage').setAttribute('aria-hidden', 'false');
      history.pushState({ matdanceBlank: true }, '', `#window-${kind}`);
    }

    function backFromBlank() {
      $('blankPage').classList.remove('active');
      $('blankPage').setAttribute('aria-hidden', 'true');
      document.querySelector('.home-shell').style.display = 'grid';
      if (location.hash.startsWith('#window-')) history.pushState(null, '', location.pathname + location.search);
    }

    async function toggleFullscreen() {
      if (!document.fullscreenElement) await document.documentElement.requestFullscreen();
      else await document.exitFullscreen();
    }

    function refreshStarMapAfterLayout() {
      if (state.activeTab !== 'home') return;
      resizeStarMap();
      requestAnimationFrame(resizeStarMap);
      setTimeout(resizeStarMap, 120);
    }

    async function returnHomeFromWindow() {
      if (document.fullscreenElement) {
        try { await document.exitFullscreen(); } catch {}
      }
      await goHome();
      refreshStarMapAfterLayout();
    }

    $('winClose').addEventListener('click', () => returnHomeFromWindow().catch(() => {}));
    $('winMin').addEventListener('click', () => returnHomeFromWindow().catch(() => {}));
    $('winMax').addEventListener('click', () => toggleFullscreen().catch(() => {}));
    $('agentWinClose').addEventListener('click', () => returnHomeFromWindow().catch(() => {}));
    $('agentWinMin').addEventListener('click', () => returnHomeFromWindow().catch(() => {}));
    $('agentWinMax').addEventListener('click', () => toggleFullscreen().catch(() => {}));
    $('settingsWinClose').addEventListener('click', function() { returnHomeFromWindow().catch(function() {}); });
    $('settingsWinMin').addEventListener('click', function() { returnHomeFromWindow().catch(function() {}); });
    $('settingsWinMax').addEventListener('click', function() { toggleFullscreen().catch(function() {}); });
    $('scheduleWinClose').addEventListener('click', function() { returnHomeFromWindow().catch(function() {}); });
    $('scheduleWinMin').addEventListener('click', function() { returnHomeFromWindow().catch(function() {}); });
    $('scheduleWinMax').addEventListener('click', function() { toggleFullscreen().catch(function() {}); });
    if ($('skillsWinClose')) $('skillsWinClose').addEventListener('click', function() { returnHomeFromWindow().catch(function() {}); });
    if ($('skillsWinMin')) $('skillsWinMin').addEventListener('click', function() { returnHomeFromWindow().catch(function() {}); });
    if ($('skillsWinMax')) $('skillsWinMax').addEventListener('click', function() { toggleFullscreen().catch(function() {}); });
    if ($('labWinClose')) $('labWinClose').addEventListener('click', function() { returnHomeFromWindow().catch(function() {}); });
    if ($('labWinMin')) $('labWinMin').addEventListener('click', function() { returnHomeFromWindow().catch(function() {}); });
    if ($('labWinMax')) $('labWinMax').addEventListener('click', function() { toggleFullscreen().catch(function() {}); });
    $('memoryWinClose').addEventListener('click', function() { returnHomeFromWindow().catch(function() {}); });
    $('memoryWinMin').addEventListener('click', function() { returnHomeFromWindow().catch(function() {}); });
    $('memoryWinMax').addEventListener('click', function() { toggleFullscreen().catch(function() {}); });
    $('blankBack').addEventListener('click', backFromBlank);
    $('matdanceTitle')?.addEventListener('click', handleMatdanceTitlePress);
    $('matdanceTitle')?.addEventListener('keydown', function(event) {
      if (event.key === 'Enter' || event.key === ' ') {
        event.preventDefault();
        handleMatdanceTitlePress(event);
      }
    });
    $('easterEggOverlay')?.addEventListener('click', hideEasterEgg);
    document.addEventListener('keydown', function(event) {
      if (event.key === 'Escape') hideEasterEgg();
    });
    window.addEventListener('popstate', () => {
      if ($('blankPage').classList.contains('active')) backFromBlank();
    });

    document.addEventListener('fullscreenchange', refreshStarMapAfterLayout);
    document.querySelectorAll('.planet-chip[data-tab]').forEach(button => {
      button.addEventListener('click', () => startWarp(button.dataset.tab));
    });
    $('agentSelect').addEventListener('change', async (e) => {
      state.agent = e.target.value;
      state.session = null;
      state.sessionReadOnly = false;
      syncAgentSelectors();
      await loadSessions();
      if (state.activeTab === 'agent') await loadAgentConfig();
      if (state.activeTab === 'schedule') await loadScheduledTasks(1);
      if (state.activeTab === 'settings' || state.activeTab === 'chat') await loadMultiModalConfig();
      if (state.activeTab === 'lab') await loadLab();
    });
    $('agentConfigSelect').addEventListener('change', async (e) => {
      state.agent = e.target.value;
      state.session = null;
      state.sessionReadOnly = false;
      syncAgentSelectors();
      await loadSessions();
      await loadAgentConfig();
      if (state.activeTab === 'settings' || state.activeTab === 'chat') await loadMultiModalConfig();
      if (state.activeTab === 'lab') await loadLab();
    });
    $('scheduleAgentSelect').addEventListener('change', async function(e) {
      state.agent = e.target.value;
      state.session = null;
      state.sessionReadOnly = false;
      syncAgentSelectors();
      await loadSessions();
      await loadScheduledTasks(1);
    });
    $('labAgentSelect')?.addEventListener('change', async function() {
      state.agent = this.value;
      state.session = null;
      state.sessionReadOnly = false;
      syncAgentSelectors();
      await loadSessions();
      await loadLab();
    });
    $('scheduleReload').addEventListener('click', function() { loadScheduledTasks().catch(function(err) { setScheduleState(err.message); }); });
    $('scheduleNew').addEventListener('click', async function() {
      const hasContent = $('scheduleTitle').value.trim() || $('scheduleContent').value.trim();
      if (!hasContent) { resetScheduleForm(); return; }
      try {
        const payload = schedulePayload();
        delete payload.taskId;
        payload.status = 'enabled';
        const task = await api('/api/scheduled-tasks', { method: 'POST', headers: { 'content-type': 'application/json' }, body: JSON.stringify(payload) });
        fillScheduleForm(task);
        await loadScheduledTasks();
        if (task.taskId) {
          const params = new URLSearchParams({ agent: state.agent, taskId: task.taskId, take: 20 });
          const data = await api('/api/scheduled-task?' + params.toString());
          renderScheduleHistory(data.runs);
        }
        setScheduleState(t('scheduleCreatedPrefix') + ': ' + task.taskId);
      } catch (err) { setScheduleState(err.message); }
    });
    initScheduleRuleEditor();
    parseScheduleJson();
    $('schedulePrev').addEventListener('click', function() { if (state.schedulePage === 1) return; loadScheduledTasks(state.schedulePage - 1).catch(function(err) { setScheduleState(err.message); }); });
    $('scheduleNext').addEventListener('click', function() { loadScheduledTasks(state.schedulePage + 1).catch(function(err) { setScheduleState(err.message); }); });
    $('scheduleForm').addEventListener('submit', async function(e) {
      e.preventDefault();
      try { await saveScheduledTask(); }
      catch (err) { setScheduleState(err.message); }
    });
    $('scheduleList').addEventListener('click', function(event) {
      const button = event.target.closest('button[data-action]');
      if (!button) return;
      const id = button.dataset.id;
      const action = button.dataset.action;
      let work = null;
      if (action === 'edit') work = readScheduledTask(id);
      if (action === 'read') work = readScheduledTask(id);
      if (action === 'delete') work = deleteScheduledTask(id);
      if (action === 'do') work = runScheduledTaskOnce(id);
      if (action === 'retry') work = retryScheduledTask(id);
      if (action === 'repair-retry') work = repairRetryScheduledTask(id);
      if (work) work.catch(function(err) { setScheduleState(err.message); });
    });
    if ($('skillsAgentSelect')) {
      $('skillsAgentSelect').addEventListener('change', async function() {
        state.agent = this.value;
        syncAgentSelectors();
        await loadSkills();
      });
    }
    if ($('skillsReload')) {
      $('skillsReload').addEventListener('click', function() { loadSkills().catch(function(err) { setSkillsState(err.message); }); });
    }
    if ($('skillsOrganize')) {
      $('skillsOrganize').addEventListener('click', function() { startSkillsOrganization(); });
    }
    if ($('skillsLearnValidate')) {
      $('skillsLearnValidate').addEventListener('click', function() { showSkillsLearnDialog(); });
    }
    document.addEventListener('click', handleSkillsLearnOverlayClick);
    document.addEventListener('change', handleSkillsLearnOverlayChange);
    document.addEventListener('DOMContentLoaded', function() { updateSkillsLang(); });
    if ($('skillsForm')) {
      $('skillsForm').addEventListener('submit', async function(e) {
        e.preventDefault();
        try { await saveSkill(); }
        catch (err) { setSkillsState(err.message); }
      });
    }
    if ($('skillNew')) {
      $('skillNew').addEventListener('click', function() { resetSkillForm(); });
    }
    if ($('skillExport')) {
      $('skillExport').addEventListener('click', function() {
        const id = $('skillId').value.trim();
        exportSkill(id).catch(function(err) { setSkillsState(t('skillsExportFailedPrefix') + ': ' + err.message); });
      });
    }
    if ($('skillDelete')) {
      $('skillDelete').addEventListener('click', async function() {
        const id = $('skillId').value.trim();
        if (!id) { setSkillsState(t('skillsSelectFirst')); return; }
        await deleteSkill(id);
      });
    }
    if ($('skillContent')) {
      $('skillContent').addEventListener('input', updateSkillPreview);
    }
    if ($('skillsList')) {
      $('skillsList').addEventListener('click', function(event) {
        const button = event.target.closest('button[data-action]');
        if (button) {
          const id = button.dataset.id;
          const action = button.dataset.action;
          if (action === 'load') readSkill(id).catch(function(err) { setSkillsState(err.message); });
          if (action === 'export') exportSkill(id).catch(function(err) { setSkillsState(t('skillsExportFailedPrefix') + ': ' + err.message); });
          if (action === 'delete') deleteSkill(id).catch(function(err) { setSkillsState(err.message); });
          if (action === 'validate') startSkillValidation(id);
          return;
        }
        const item = event.target.closest('.skill-item');
        if (item) {
          readSkill(item.dataset.id).catch(function(err) { setSkillsState(err.message); });
        }
      });
    }
    document.querySelectorAll('[data-memory-tab]').forEach(function(btn) {
      btn.addEventListener('click', function() { switchMemoryTab(btn.dataset.memoryTab); });
    });
    $('memoryAgentSelect')?.addEventListener('change', async function() {
      state.agent = this.value;
      state.memoryVectorAtlas = null;
      state.memoryVectorResults = null;
      state.memoryVectorHover = null;
      state.memoryVectorPinned = null;
      syncAgentSelectors();
      await loadMemory();
    });
    $('memoryReload')?.addEventListener('click', function() { loadMemory().catch(function(err) { setMemoryState(err.message); }); });
    async function startMemoryOrganization(forceFullRebuild) {
      if (state.memoryOrganizing) return;
      const limits = loadMemoryLimits();
      try {
        state.memoryOrganizing = true;
        setMemoryState(t('memoryOrganizeChecking'));
        showOrganizeProgress(0, t('memoryOrganizePrepare'), t('memoryOrganizeTitle'), t('memoryOrganizeDesc'));
        
        const res = await api('/api/memory/organize', {
          method: 'POST',
          headers: { 'content-type': 'application/json' },
          body: JSON.stringify({
            agent: state.agent,
            hotMemoryLimit: limits.hot,
            coreMemoryLimit: limits.core,
            userMdLimit: limits.user,
            identityMdLimit: limits.identity,
            forceFullRebuild: !!forceFullRebuild
          })
        });
        
        if (res.status === 'busy') {
          setMemoryState(t('memoryOrganizeBusy'));
          hideOrganizeProgress();
          state.memoryOrganizing = false;
          return;
        }
        
        if (res.status === 'started' && res.jobId) {
          setMemoryState(t('memoryOrganizeStarted'));
          pollOrganizationStatus(res.jobId);
        }
      } catch (err) {
        setMemoryState(t('memoryOrganizeStartFailedPrefix') + ': ' + err.message);
        hideOrganizeProgress();
        state.memoryOrganizing = false;
      }
    }
    $('memoryOrganize')?.addEventListener('click', function() { startMemoryOrganization(false); });
    $('memoryOrganizeFull')?.addEventListener('click', function() { startMemoryOrganization(true); });
    $('memorySaveCore')?.addEventListener('click', function() { saveCoreMemory().catch(function(err) { setMemoryState(err.message); }); });
    $('memorySnapshotRestore')?.addEventListener('click', function() { restoreMemorySnapshot().catch(function(err) { setMemoryState(err.message); }); });
    $('memoryLtmStartDate')?.addEventListener('change', function() { loadLongTermMemory(1).catch(function(err) { setMemoryState(err.message); }); });
    $('memoryLtmEndDate')?.addEventListener('change', function() { loadLongTermMemory(1).catch(function(err) { setMemoryState(err.message); }); });
    $('memoryLtmResetBtn')?.addEventListener('click', function() {
      if ($('memoryLtmStartDate')) $('memoryLtmStartDate').value = '';
      if ($('memoryLtmEndDate')) $('memoryLtmEndDate').value = '';
      loadLongTermMemory(1).catch(function(err) { setMemoryState(err.message); });
    });
    $('memoryLtmPrev')?.addEventListener('click', function() { if (state.memoryPage === 1) return; loadLongTermMemory(state.memoryPage - 1).catch(function(err) { setMemoryState(err.message); }); });
    $('memoryLtmNext')?.addEventListener('click', function() { loadLongTermMemory(state.memoryPage + 1).catch(function(err) { setMemoryState(err.message); }); });
    $('memoryLtmDelete')?.addEventListener('click', function() { deleteLongTermMemory().catch(function(err) { setMemoryState(err.message); }); });
    $('memoryVectorSearchBtn')?.addEventListener('click', function() { searchVectorMemory().catch(function(err) { setMemoryState(err.message); }); });
    $('memoryVectorQuery')?.addEventListener('keydown', function(event) {
      if (event.key === 'Enter') {
        event.preventDefault();
        searchVectorMemory().catch(function(err) { setMemoryState(err.message); });
      }
    });
    $('memoryVectorCanvas')?.addEventListener('mousemove', handleVectorCanvasMove);
    $('memoryVectorCanvas')?.addEventListener('mouseleave', function() {
      state.memoryVectorHover = null;
      renderVectorAtlas();
    });
    $('memoryVectorCanvas')?.addEventListener('click', handleVectorCanvasClick);
    window.addEventListener('resize', function() {
      if (state.activeTab === 'memory' && state.memoryTab === 'vector') renderVectorAtlas();
      handleChatScroll();
    });
    $('chat')?.addEventListener('scroll', handleChatScroll, { passive: true });
    $('chat')?.addEventListener('wheel', noteChatUserScrollIntent, { passive: true });
    $('chat')?.addEventListener('touchstart', noteChatUserScrollIntent, { passive: true });
    $('chat')?.addEventListener('pointerdown', noteChatUserScrollIntent, { passive: true });
    $('chatJumpBottom')?.addEventListener('click', jumpChatToBottom);
    window.addEventListener('pointerdown', primeSoundCueAudio, { passive: true });
    window.addEventListener('keydown', primeSoundCueAudio);
    $('sessionSelect').addEventListener('change', async (e) => { state.session = e.target.value; await loadSession(); });
    $('newSession').addEventListener('click', createSession);
    $('stopResponse').addEventListener('click', function() { if (state.abortController) { $('hint').textContent = t('stopping'); state.abortController.abort(); } });
    $('langToggle').addEventListener('click', function() { state.lang = state.lang === 'zh' ? 'en' : 'zh'; localStorage.setItem('matdanceLang', state.lang); applyI18n(); });
    document.querySelectorAll('[data-settings-section]').forEach(function(button) {
      button.addEventListener('click', function() { switchSettingsSection(button.dataset.settingsSection); });
    });
    $('runtimeEventsReload')?.addEventListener('click', function() { loadRuntimeEvents().catch(function(err) { const list = $('runtimeEventsList'); if (list) list.textContent = err.message; }); });
    $('runtimeEventsAgentSelect')?.addEventListener('change', function() {
      state.runtimeEventsAgent = this.value || null;
      loadRuntimeEvents().catch(function(err) { const list = $('runtimeEventsList'); if (list) list.textContent = err.message; });
    });
    // Load memory limits from localStorage
    function loadMemoryLimits() {
      const defaults = { hot: 10000, core: 15000, user: 5000, identity: 2000 };
      try {
        const stored = JSON.parse(localStorage.getItem('matdanceMemoryLimits') || '{}');
        return { ...defaults, ...stored };
      } catch { return defaults; }
    }
    function saveMemoryLimits() {
      const limits = {
        hot: parseInt($('settingsHotLimit')?.value || '10000', 10),
        core: parseInt($('settingsCoreLimit')?.value || '15000', 10),
        user: parseInt($('settingsUserLimit')?.value || '5000', 10),
        identity: parseInt($('settingsIdentityLimit')?.value || '2000', 10)
      };
      localStorage.setItem('matdanceMemoryLimits', JSON.stringify(limits));
      return limits;
    }
    function updateMemoryLimitReadouts(limits = loadMemoryLimits()) {
      const map = {
        settingsHotReadout: limits.hot,
        settingsCoreReadout: limits.core,
        settingsUserReadout: limits.user,
        settingsIdentityReadout: limits.identity
      };
      Object.entries(map).forEach(function(entry) {
        const node = $(entry[0]);
        if (node) node.textContent = Number(entry[1] || 0).toLocaleString();
      });
    }
    const memoryLimits = loadMemoryLimits();
    if ($('settingsHotLimit')) $('settingsHotLimit').value = memoryLimits.hot;
    if ($('settingsCoreLimit')) $('settingsCoreLimit').value = memoryLimits.core;
    if ($('settingsUserLimit')) $('settingsUserLimit').value = memoryLimits.user;
    if ($('settingsIdentityLimit')) $('settingsIdentityLimit').value = memoryLimits.identity;
    updateMemoryLimitReadouts(memoryLimits);
    ['settingsHotLimit','settingsCoreLimit','settingsUserLimit','settingsIdentityLimit'].forEach(function(id) {
      $(id)?.addEventListener('input', function() { updateMemoryLimitReadouts(saveMemoryLimits()); });
    });
    $('settingsSaveLimits')?.addEventListener('click', function() {
      updateMemoryLimitReadouts(saveMemoryLimits());
      const btn = $('settingsSaveLimits');
      const old = btn.textContent;
      btn.textContent = t('saved');
      setTimeout(function() { btn.textContent = old; }, 1500);
    });
    $('privacyAccessToggle')?.addEventListener('change', function() {
      saveSecuritySettingsFromUi();
    });
    ['skillValidationEnabled','skillValidationIntervalHours','skillValidationBatchSize'].forEach(function(id) {
      $(id)?.addEventListener('change', function() {
        saveSkillValidationSettingsFromUi();
      });
    });
    $('soundCueEnabled')?.addEventListener('change', function(event) {
      const settings = loadSoundCueSettings();
      settings.enabled = !!event.target.checked;
      saveSoundCueSettings();
      renderSoundCueSettings();
    });
    $('soundCueVolume')?.addEventListener('input', function(event) {
      const settings = loadSoundCueSettings();
      settings.volume = Math.max(0, Math.min(1, Number(event.target.value || 0) / 100));
      saveSoundCueSettings();
      renderSoundCueSettings();
    });
    $('soundCueDelay')?.addEventListener('input', function(event) {
      const settings = loadSoundCueSettings();
      settings.delayMs = Math.max(0, Math.min(30000, Number(event.target.value || 0)));
      saveSoundCueSettings();
      renderSoundCueSettings();
    });
    $('soundCueUploadInput')?.addEventListener('change', function(event) {
      const files = Array.from(event.target.files || []);
      event.target.value = '';
      uploadSoundCueFiles(files).catch(function(err) { $('hint').textContent = t('soundCueUploadFailedPrefix') + ': ' + (err.message || String(err)); });
    });
    $('soundCueImportInput')?.addEventListener('change', function(event) {
      const file = event.target.files?.[0] || null;
      event.target.value = '';
      importSoundCueSettingsFile(file).catch(function(err) { $('hint').textContent = t('soundCueImportFailedPrefix') + ': ' + (err.message || String(err)); });
    });
    $('soundCueImport')?.addEventListener('click', function() { $('soundCueImportInput')?.click(); });
    $('soundCueExport')?.addEventListener('click', function() { exportSoundCueSettings().catch(function(err) { $('hint').textContent = t('soundCueExportFailedPrefix') + ': ' + (err.message || String(err)); }); });
    $('multiReload')?.addEventListener('click', function() { loadMultiModalConfig(); });
    $('multiSave')?.addEventListener('click', function() { saveMultiModalConfig().catch(function(err) { const s = $('multiStatus'); if (s) s.textContent = err.message; }); });
    document.addEventListener('click', function(event) {
      const groupTab = event.target.closest?.('[data-sound-group]');
      if (groupTab) {
        state.soundCueGroup = groupTab.dataset.soundGroup || 'flow';
        renderSoundCueSettings();
        return;
      }
      const previewCue = event.target.closest?.('[data-sound-preview]');
      if (previewCue) {
        playSoundCue(previewCue.dataset.soundPreview, { force: true });
        return;
      }
      const previewItem = event.target.closest?.('[data-sound-preview-item]');
      if (previewItem) {
        playSoundCue(previewItem.dataset.soundPreviewType, { force: true, itemId: previewItem.dataset.soundPreviewItem });
        return;
      }
      const uploadCue = event.target.closest?.('[data-sound-upload]');
      if (uploadCue) {
        state.soundCueUploadType = uploadCue.dataset.soundUpload;
        $('soundCueUploadInput')?.click();
        return;
      }
      const removeCue = event.target.closest?.('[data-sound-remove]');
      if (removeCue) {
        removeSoundCueAsset(removeCue.dataset.soundType, removeCue.dataset.soundRemove);
        return;
      }
      const customAdd = event.target.closest?.('[data-sound-custom-add]');
      if (customAdd) {
        addCustomSoundCueType();
        return;
      }
      const customSave = event.target.closest?.('[data-sound-custom-save]');
      if (customSave) {
        updateCustomSoundCueType(customSave.dataset.soundCustomSave);
        return;
      }
      const customDelete = event.target.closest?.('[data-sound-custom-delete]');
      if (customDelete) {
        deleteCustomSoundCueType(customDelete.dataset.soundCustomDelete);
        return;
      }
      const add = event.target.closest?.('[data-add-image-profile]');
      if (add) {
        addImageProfile();
        return;
      }
      const addTts = event.target.closest?.('[data-add-tts-profile]');
      if (addTts) {
        addTtsProfile();
        return;
      }
      const addSearch = event.target.closest?.('[data-add-search-profile]');
      if (addSearch) {
        addSearchProfile();
        return;
      }
      const remove = event.target.closest?.('[data-remove-image-profile]');
      if (remove) {
        removeImageProfile(Number(remove.dataset.removeImageProfile || '0'));
        return;
      }
      const removeTts = event.target.closest?.('[data-remove-tts-profile]');
      if (removeTts) {
        removeTtsProfile(Number(removeTts.dataset.removeTtsProfile || '0'));
        return;
      }
      const removeSearch = event.target.closest?.('[data-remove-search-profile]');
      if (removeSearch) {
        removeSearchProfile(Number(removeSearch.dataset.removeSearchProfile || '0'));
      }
    });
    document.addEventListener('change', function(event) {
      const target = event.target;
      if (target?.dataset?.soundToggle) {
        setSoundCueTypeEnabled(target.dataset.soundToggle, target.checked);
        return;
      }
      if (target?.dataset?.soundItemToggle) {
        setSoundCueAssetEnabled(target.dataset.soundType, target.dataset.soundItemToggle, target.checked);
        return;
      }
      if (!target?.id || !target.id.includes('TtsModel') || !target.id.endsWith('EndpointMode') || !state.multimodal) return;
      state.multimodal.global = readMultiProfile('multiGlobal');
      renderMultiModalSettings();
    });
    $('micButton')?.addEventListener('click', function() { setVoiceMode(!state.voiceMode); });
    $('voiceHold')?.addEventListener('pointerdown', startVoiceHold);
    $('voiceHold')?.addEventListener('pointermove', moveVoiceHold);
    $('voiceHold')?.addEventListener('pointerup', function(event) { finishVoiceHold(event).catch(function(err) { $('hint').textContent = t('sttFailedPrefix') + ': ' + (err.message || String(err)); }); });
    $('voiceHold')?.addEventListener('pointercancel', function(event) { finishVoiceHold(event, true).catch(function(err) { $('hint').textContent = t('sttFailedPrefix') + ': ' + (err.message || String(err)); }); });
    $('voiceHold')?.addEventListener('contextmenu', function(event) { event.preventDefault(); });
    $('labReload')?.addEventListener('click', function() { loadLab().catch(function(err) { $('labState').textContent = err.message; }); });
    $('labImageRun')?.addEventListener('click', function() { runLabImage(); });
    $('labTtsRun')?.addEventListener('click', function() { runLabTts(); });
    $('labSttRun')?.addEventListener('click', function() { runLabSttFile(); });
    $('labSttRecord')?.addEventListener('click', function() { toggleLabRecording().catch(function(err) { $('labSttResult').textContent = err.message || String(err); }); });
    applyI18n();
    $('refresh').addEventListener('click', function() { loadAgents().catch(function(err) { showEmpty(t('refreshFailedPrefix') + ': ' + err.message); }); });
    $('agentReload').addEventListener('click', function() { loadAgentConfig().catch(function(err) { setAgentConfigState(t('loadFailedPrefix') + ': ' + err.message); }); });
    $('agentFormReload').addEventListener('click', function() { loadAgentConfig().catch(function(err) { setAgentConfigState(t('loadFailedPrefix') + ': ' + err.message); }); });
    $('agentSaveTop').addEventListener('click', function() { saveAgentConfig().catch(function(err) { setAgentConfigState(t('saveFailedPrefix') + ': ' + err.message); }); });
    $('agentCreate').addEventListener('click', function() { createAgentFromPrompt().catch(function(err) { setAgentConfigState(err.message); }); });
    $('agentDelete').addEventListener('click', function() { deleteCurrentAgent().catch(function(err) { setAgentConfigState(err.message); }); });
    $('configApiType').addEventListener('change', function() { syncProviderDefaults(true); setModelMenuOpen(false); });
    $('configModelToggle')?.addEventListener('click', function(event) { event.preventDefault(); toggleModelMenu(); });
    $('configModelId').addEventListener('focus', function() { toggleModelMenu(true); });
    $('configModelId').addEventListener('input', function() {
      syncProviderDefaults(false);
      fillModelOptions($('configApiType').value, $('configModelId').value);
      setModelMenuOpen(true);
    });
    $('configModelId').addEventListener('keydown', handleModelComboKeydown);
    document.addEventListener('click', function(event) {
      if (!$('configModelCombo')?.contains(event.target)) setModelMenuOpen(false);
    });
    $('agentIconUpload').addEventListener('click', function() { $('agentIconInput')?.click(); });
    $('agentIconInput').addEventListener('change', function() { uploadAgentIcon().catch(function(err) { setAgentConfigState(err.message); }); });
    $('agentForm').addEventListener('submit', async (e) => {
      e.preventDefault();
      try { await saveAgentConfig(); }
      catch (err) { setAgentConfigState(t('saveFailedPrefix') + ': ' + err.message); }
    });
    $('composer').addEventListener('submit', async (e) => {
      e.preventDefault();
      const text = $('input').value.trim();
      const hasAttachments = (state.chatAttachments || []).length > 0;
      if ((!text && !hasAttachments) || !state.agent || !state.session) return;
      if (state.sessionReadOnly) {
        $('hint').textContent = t('sessionReadOnlyHint');
        return;
      }
      if (await runSlashCommand(text)) return;
      if (state.busy) return;
      $('input').value = '';
      hideCommandMenu();
      try { await sendMessage(text); }
      catch (err) { stopMatrix(); setBusy(false); addMessage('assistant', `[error] ${err.message}`, assistantMeta()); }
    });

    $('attachButton')?.addEventListener('click', function() {
      if (state.sessionReadOnly) { $('hint').textContent = t('sessionReadOnlyHint'); return; }
      $('chatAttachmentInput')?.click();
    });
    $('chatAttachmentInput')?.addEventListener('change', function(event) {
      addChatAttachmentFiles(event.target.files || []);
      event.target.value = '';
    });
    $('composer')?.addEventListener('dragover', function(event) {
      if (!event.dataTransfer?.files?.length) return;
      event.preventDefault();
    });
    $('composer')?.addEventListener('drop', function(event) {
      if (!event.dataTransfer?.files?.length) return;
      event.preventDefault();
      addChatAttachmentFiles(event.dataTransfer.files);
    });
    $('input')?.addEventListener('paste', function(event) {
      const files = event.clipboardData?.files;
      if (!files?.length) return;
      addChatAttachmentFiles(files);
    });

    $('input').addEventListener('input', () => {
      state.commandIndex = 0;
      renderCommandMenu();
    });

    $('input').addEventListener('blur', () => setTimeout(hideCommandMenu, 120));

    $('input').addEventListener('keydown', (e) => {
      const menuActive = !$('commandMenu').hidden;
      if (menuActive && e.key === 'ArrowDown') {
        e.preventDefault();
        const matches = commandMatches();
        state.commandIndex = matches.length ? (state.commandIndex + 1) % matches.length : 0;
        renderCommandMenu();
        return;
      }
      if (menuActive && e.key === 'ArrowUp') {
        e.preventDefault();
        const matches = commandMatches();
        state.commandIndex = matches.length ? (state.commandIndex - 1 + matches.length) % matches.length : 0;
        renderCommandMenu();
        return;
      }
      if (menuActive && (e.key === 'Tab' || e.key === 'ArrowRight')) {
        e.preventDefault();
        completeCommand();
        return;
      }
      if (e.key === 'Enter' && !e.shiftKey) {
        e.preventDefault();
        if (menuActive && commandToken() && !findCommand(commandToken())) {
          completeCommand();
          return;
        }
        $('composer').requestSubmit();
      }
    });

    // Browser overlay controls
    function openBrowserOverlay() { $('browserOverlay').classList.add('active'); state.browserOpen = true; connectBrowserWs(); }
    function closeBrowserOverlay() { $('browserOverlay').classList.remove('active'); state.browserOpen = false; disconnectBrowserWs(); }
    function minimizeBrowserOverlay() { $('browserOverlay').classList.remove('active'); state.browserOpen = false; }
    function toggleBrowserMaximize() {
      state.browserMaximized = !state.browserMaximized;
      const win = $('browserWindow');
      if (state.browserMaximized) {
        win.style.width = '100vw'; win.style.height = '100vh'; win.style.borderRadius = '0';
      } else {
        win.style.width = ''; win.style.height = ''; win.style.borderRadius = '';
      }
    }

    function connectBrowserWs() {
      if (state.browserWs) return;
      const protocol = location.protocol === 'https:' ? 'wss:' : 'ws:';
      const ws = new WebSocket(protocol + '//' + location.host + '/ws/browser');
      ws.binaryType = 'arraybuffer';
      ws.onopen = function() {
        $('browserPlaceholder').classList.add('hidden');
        $('browserFrame').classList.add('active');
      };
      ws.onmessage = function(ev) {
        if (ev.data instanceof ArrayBuffer) {
          const blob = new Blob([ev.data], { type: 'image/jpeg' });
          const url = URL.createObjectURL(blob);
          const img = $('browserFrame');
          img.onload = function() { URL.revokeObjectURL(url); };
          img.src = url;
        }
      };
      ws.onclose = function() {
        $('browserPlaceholder').classList.remove('hidden');
        $('browserFrame').classList.remove('active');
        state.browserWs = null;
        // Auto-reconnect if overlay is still open (browser may have restarted)
        if (state.browserOpen) {
          setTimeout(connectBrowserWs, 1500);
        }
      };
      ws.onerror = function() {
        state.browserWs = null;
      };
      state.browserWs = ws;
    }

    function disconnectBrowserWs() {
      if (state.browserWs) {
        try { state.browserWs.close(); } catch(e) {}
        state.browserWs = null;
      }
    }

    $('browserBtn').addEventListener('click', openBrowserOverlay);
    $('browserWinClose').addEventListener('click', closeBrowserOverlay);
    $('browserWinMin').addEventListener('click', minimizeBrowserOverlay);
    $('browserWinMax').addEventListener('click', toggleBrowserMaximize);
    $('ttsErrorOverlay')?.addEventListener('click', function(event) {
      if (event.target === this) hideTtsErrorOverlay();
    });

    initStarMap();
    syncBrowserTimeZone();
    loadAgents().catch(function(err) { showEmpty(t('loadFailedPrefix') + ': ' + err.message); });
  </script>

  <div id="ttsErrorOverlay" class="tts-error-overlay" aria-hidden="true">
    <div class="tts-error-card" role="alertdialog" aria-modal="true" aria-labelledby="ttsErrorTitle">
      <strong id="ttsErrorTitle">Speech Playback Failed</strong>
      <p id="ttsErrorMessage"></p>
    </div>
  </div>

  <!-- Organize Progress Overlay -->
  <div id="skillsLearnOverlay" class="skills-learn-overlay" aria-hidden="true">
    <div class="skills-learn-dialog" role="dialog" aria-modal="true" aria-labelledby="skillsLearnTitle">
      <div class="skills-learn-head">
        <div><strong id="skillsLearnTitle">Learn and validate external skill</strong><p id="skillsLearnDesc">Paste external skill text or provide a local file/folder path. External material is treated as untrusted input.</p></div>
        <button id="skillsLearnClose" class="skills-learn-close" type="button" aria-label="Close" title="Close">&times;</button>
      </div>
      <div class="skills-learn-body">
        <div class="field"><label for="skillsLearnNameHint"><span id="skillsLearnNameHintLabel">Name hint</span><span id="skillsLearnNameHintMeta">optional</span></label><input id="skillsLearnNameHint" type="text" autocomplete="off" /></div>
        <div class="field"><label for="skillsLearnPath"><span id="skillsLearnPathLabel">Local path</span><span id="skillsLearnPathMeta">file or folder</span></label><input id="skillsLearnPath" type="text" autocomplete="off" /></div>
        <div class="field">
          <label><span id="skillsLearnFileLabel">Files</span><span id="skillsLearnFileMeta">folder, zip, txt, md</span></label>
          <div class="skills-learn-picker-row">
            <button id="skillsLearnChooseFiles" class="ghost" type="button">Choose files/package</button>
            <button id="skillsLearnChooseFolder" class="ghost" type="button">Choose folder</button>
            <button id="skillsLearnClearFiles" class="ghost" type="button">Clear</button>
          </div>
          <div id="skillsLearnSelected" class="skills-learn-file-summary">No files selected.</div>
          <input id="skillsLearnFileInput" class="skills-learn-hidden-input" type="file" multiple accept=".zip,.txt,.md,.markdown,.json,.yaml,.yml,.toml,.xml,.html,.htm,.css,.js,.mjs,.cjs,.ts,.tsx,.jsx,.py,.ps1,.sh,.bat,.cmd,.csv,.ini" />
          <input id="skillsLearnFolderInput" class="skills-learn-hidden-input" type="file" webkitdirectory directory multiple />
        </div>
        <div class="field"><label for="skillsLearnText"><span id="skillsLearnTextLabel">External material</span><span id="skillsLearnTextMeta">untrusted</span></label><textarea id="skillsLearnText"></textarea></div>
      </div>
      <div class="skills-learn-actions">
        <button id="skillsLearnCancel" class="ghost" type="button">Cancel</button>
        <button id="skillsLearnStart" class="primary" type="button">Start</button>
      </div>
    </div>
  </div>

  <div id="organizeOverlay" class="organize-overlay" aria-hidden="true">
    <div class="organize-card">
      <h3 id="organizeTitle">整理记忆中...</h3>
      <p id="organizeDesc">正在分析会话并整理记忆文件</p>
      <div class="organize-progress"><div id="organizeProgressBar" class="organize-progress-bar"></div></div>
      <div id="organizePercent" class="organize-percent">0%</div>
      <div id="organizeStage" class="organize-stage">准备中...</div>
      <div id="organizeError" class="organize-error"></div>
    </div>
  </div>
  <div id="scheduleTestOverlay" class="schedule-test-overlay" aria-hidden="true">
    <div class="schedule-test-card" role="status" aria-live="polite">
      <strong id="scheduleTestTitle">Testing scheduled task</strong>
      <p id="scheduleTestStage">Reading run progress...</p>
      <div class="schedule-test-bar"><span></span></div>
      <small id="scheduleTestDetail">This test does real work and delivers results.</small>
    </div>
  </div>
</body>
</html>
""";
}
