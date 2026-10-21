// Requires Playwright. Uses an isolated headless browser, --mute-audio and the
// game's silent-test entry together. Never changes the Mac's system volume.
const {chromium} = require("playwright");
const fs = require("node:fs");
const assert = require("node:assert/strict");
const base = process.env.POTATO_TEST_URL || "http://127.0.0.1:8919";
const report = [];
const check = (value, text) => { assert.ok(value, text); report.push("PASS " + text); };

(async () => {
  const browser = await chromium.launch({
    headless: true, args: ["--mute-audio", "--autoplay-policy=no-user-gesture-required"]
  });
  try {
    const context = await browser.newContext({viewport: {width: 1440, height: 900}});
    const errors = [];
    await context.addInitScript(() => {
      const audit = window.audioAudit = {contexts: [], sources: [], maxMusic: 0, blockResume: false};
      const AC = window.AudioContext;
      const resume = AC.prototype.resume;
      AC.prototype.resume = function() { return audit.blockResume ? Promise.resolve() : resume.call(this); };
      window.AudioContext = class extends AC {
        constructor(...args) { super(...args); audit.contexts.push(this); }
      };
      const create = AC.prototype.createMediaElementSource;
      AC.prototype.createMediaElementSource = function(media) {
        const node = create.call(this, media), record = {media, connected: false};
        audit.sources.push(record);
        const connect = node.connect.bind(node), disconnect = node.disconnect.bind(node);
        node.connect = (...args) => { record.connected = true; return connect(...args); };
        node.disconnect = (...args) => { record.connected = false; return disconnect(...args); };
        return node;
      };
      audit.playing = () => audit.sources.filter(s => s.connected && !s.media.paused && s.media.duration > 10);
      setInterval(() => { audit.maxMusic = Math.max(audit.maxMusic, audit.playing().length); }, 20);
    });
    const open = async (startGame = true) => {
      const p = await context.newPage();
      p.on("pageerror", error => errors.push(error.message));
      p.on("console", message => { if (message.type() === "error") errors.push(message.text()); });
      await p.goto(base + "/?silent-test=1");
      await p.waitForFunction(() => window.game && window.potatoAudioFocus, null, {timeout: 90000});
      await p.waitForTimeout(2500);
      await p.mouse.click(40, 40, {delay: 100});
      await p.waitForTimeout(300);
      if (startGame) await p.mouse.click(340, 653, {delay: 100});
      await p.waitForTimeout(1500);
      return p;
    };
    const click = async (p, x, y) => { await p.mouse.click(x, y, {delay: 100}); await p.waitForTimeout(250); };
    const playing = p => p.evaluate(() => audioAudit.playing().length);
    const focus = async p => {
      await p.bringToFront();
      // Headless pages report hasFocus() for multiple tabs; deliver the paired
      // focus event after the synthetic blur used above.
      await p.evaluate(() => window.dispatchEvent(new Event("focus")));
      await click(p, 40, 40);
      await p.waitForTimeout(1500);
    };
    let page = await open();
    check(await page.evaluate(() => potatoAudioFocus.silent), "browser output gate is silent throughout testing");
    check(await playing(page) === 1, "one real browser music element after startup");
    await click(page, 1350, 50);
    await page.screenshot({path: "/tmp/potato-audio-settings.png"});
    for (let i = 0; i < 3; i++) { await click(page, 1098, 557); await page.waitForTimeout(1700); }
    for (let i = 0; i < 9; i++) {
      await click(page, 1098, 557);
      await page.waitForTimeout(i % 3 === 0 ? 1600 : 50);
    }
    await page.waitForTimeout(1700);
    check(await playing(page) === 1, "rapid next-track clicks settle to one connected media element");
    check(await page.evaluate(() => new Set(audioAudit.sources.map(s => Math.round(s.media.duration)).filter(Number.isFinite)).size >= 3),
      "browser actually played multiple different playlist tracks");
    await page.evaluate(async () => { audioAudit.blockResume = true; await audioAudit.contexts[0].suspend(); });
    await click(page, 1098, 557); await click(page, 1098, 557);
    await page.evaluate(async () => { audioAudit.blockResume = false; await audioAudit.contexts[0].resume(); });
    await page.waitForTimeout(1800);
    check(await playing(page) === 1, "audio-context resume does not leave a previous song connected");
    await page.evaluate(() => window.dispatchEvent(new Event("blur")));
    await page.waitForTimeout(300);
    check(await playing(page) === 0 && !await page.evaluate(() => potatoAudioFocus.allowed),
      "window blur stops the actual browser player and closes output gate");
    await focus(page);
    check(await playing(page) === 1, "returning to window resumes one track");
    await click(page, 720, 440); // disable automatic repeat trips
    await click(page, 1137, 133); // close settings
    await click(page, 190, 834); // direction
    await page.screenshot({path: "/tmp/potato-audio-direction.png"});
    // Four direction cards; the first one's selection button.
    await click(page, 430, 427);
    await click(page, 800, 834); // depart
    await page.waitForTimeout(2000);
    await page.screenshot({path: "/tmp/potato-audio-away.png"});
    check(await playing(page) === 1, "departure keeps a single connected browser music player");
    check(await page.evaluate(() => Math.round(audioAudit.playing()[0].media.duration) === 193),
      "departure actually changed music to Winding Path");
    await page.waitForTimeout(31000);
    await click(page, 800, 834); // collect
    await page.waitForTimeout(1800);
    check(await playing(page) === 1, "return and collection keep a single music player");
    check(await page.evaluate(() => Math.round(audioAudit.playing()[0].media.duration) !== 193),
      "arrival actually restored the home playlist");
    await page.screenshot({path: "/tmp/potato-audio-return.png"});
    const second = await open(false);
    await focus(second);
    check(await playing(page) === 0 && await playing(second) === 1,
      "second game window takes audio ownership and silences the first");
    await focus(page);
    check(await playing(page) === 1 && await playing(second) === 0,
      "switching back transfers ownership without two songs");
    check(await page.evaluate(() => audioAudit.maxMusic <= 1), "20ms sampling found no overlapping music in first window");
    check(await second.evaluate(() => audioAudit.maxMusic <= 1), "20ms sampling found no overlapping music in second window");
    await page.close();
    await focus(second);
    check(await playing(second) === 1, "closing previous owner does not lock other window's audio");
    check(errors.length === 0, "browser error log empty: " + errors.join("; "));
    report.push("WEB_AUDIO_OK (all physical output muted)");
  } catch (error) {
    for (const p of browser.contexts()[0]?.pages() || []) {
      await p.screenshot({path: "/tmp/potato-audio-failed.png"}).catch(() => {});
      report.push(JSON.stringify(await p.evaluate(() => ({
        allowed: window.potatoAudioFocus?.allowed,
        sources: window.audioAudit?.sources.map(s => ({connected: s.connected, paused: s.media.paused, duration: s.media.duration}))
      })).catch(() => ({}))));
    }
    report.push("FAILED " + error.stack);
    process.exitCode = 1;
  } finally {
    await browser.close();
    fs.writeFileSync("docs/audio-web-v0.1.2.txt", report.join("\n") + "\n");
    console.log(report.join("\n"));
  }
})();
