// Gate the actual Web Audio output as well as Unity playback. Hidden/throttled
// tabs cannot keep sounding while another game window owns the music.
(() => {
  "use strict";
  const id = crypto.randomUUID();
  const silent = new URLSearchParams(location.search).get("silent-test") === "1";
  const gates = new WeakMap();
  const outputs = [];
  const nativeConnect = AudioNode.prototype.connect;
  const NativeContext = window.AudioContext || window.webkitAudioContext;
  let game, allowed = false, claimTimer, disposed = false;
  const channel = typeof BroadcastChannel === "function"
    ? new BroadcastChannel("PotatoHK.AudioFocus") : null;

  function setAllowed(value) {
    allowed = value && !disposed;
    for (const gain of outputs) gain.gain.value = allowed && !silent ? 1 : 0;
    if (game) game.SendMessage("Potato life", "SetWebAudioFocus", allowed ? "1" : "0");
  }
  function active() { return !document.hidden && document.hasFocus() && !disposed; }
  function claim() {
    clearTimeout(claimTimer);
    setAllowed(false);
    if (!active()) return;
    if (channel) channel.postMessage({type: "claim", id});
    // Give other same-origin windows time to stop before enabling this output.
    claimTimer = setTimeout(() => { if (active()) setAllowed(true); }, 100);
  }
  function yieldAudio() { clearTimeout(claimTimer); setAllowed(false); }
  if (channel) channel.onmessage = event => {
    if (event.data && event.data.type === "claim" && event.data.id !== id) yieldAudio();
  };

  if (NativeContext) {
    class FocusContext extends NativeContext {
      constructor(...args) {
        super(...args);
        const gain = this.createGain();
        gain.gain.value = allowed && !silent ? 1 : 0;
        gates.set(this, gain);
        outputs.push(gain);
        nativeConnect.call(gain, this.destination);
      }
    }
    window.AudioContext = FocusContext;
    if (window.webkitAudioContext) window.webkitAudioContext = FocusContext;
    AudioNode.prototype.connect = function(destination, ...args) {
      const gain = gates.get(this.context);
      if (gain && destination === this.context.destination && this !== gain) {
        nativeConnect.call(this, gain, ...args);
        return destination;
      }
      return nativeConnect.call(this, destination, ...args);
    };
  }
  window.addEventListener("focus", claim);
  window.addEventListener("blur", yieldAudio);
  document.addEventListener("visibilitychange", () => document.hidden ? yieldAudio() : claim());
  window.addEventListener("pointerdown", () => { if (!allowed && active()) claim(); }, true);
  window.addEventListener("pagehide", () => {
    disposed = true;
    yieldAudio();
    if (channel) channel.close();
  });
  window.addEventListener("pageshow", event => { if (event.persisted) location.reload(); });
  window.potatoAudioFocus = {
    attach(instance) { game = instance; setAllowed(false); claim(); },
    get allowed() { return allowed; },
    get silent() { return silent; }
  };
})();
