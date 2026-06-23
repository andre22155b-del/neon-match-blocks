import { useCallback, useEffect, useMemo, useReducer, useRef, useState } from 'react';
import { cssVars } from './constants/colors';
import {
  CLEAR_MS,
  COLS,
  COMBO_RESET_MS,
  TIMED_SECONDS,
  PRESTIGE_WORDS,
  findMatches,
  getScoringMatchesForState,
  initialState,
  makeTile,
  modes,
  reducer,
} from './game/logic';

const tone = {
  purple: 'tile-purple',
  cyan: 'tile-cyan',
  magenta: 'tile-magenta',
  ai: 'tile-ai',
};

const modeStyles = {
  classic: { icon: '♾', accent: 'var(--color-purpleEnd)', glow: 'var(--color-purpleModeHoverStrong)', softGlow: 'var(--color-purpleModeHoverSoft)', bg: 'var(--color-purpleModeBg)', buttonBg: 'var(--color-purpleButtonBg)', buttonHover: 'var(--color-purpleButtonBgHover)' },
  timed: { icon: '⚡', accent: 'var(--color-magenta)', glow: 'var(--color-magentaModeHoverStrong)', softGlow: 'var(--color-magentaModeHoverSoft)', bg: 'var(--color-magentaModeBg)', buttonBg: 'var(--color-magentaButtonBg)', buttonHover: 'var(--color-magentaButtonBgHover)' },
  puzzle: { icon: '◈', accent: 'var(--color-cyan)', glow: 'var(--color-cyanModeHoverStrong)', softGlow: 'var(--color-cyanModeHoverSoft)', bg: 'var(--color-cyanModeBg)', buttonBg: 'var(--color-cyanButtonBg)', buttonHover: 'var(--color-cyanButtonBgHover)' },
  tutorial: { icon: '◎', accent: 'var(--color-gold)', glow: 'var(--color-goldModeHoverStrong)', softGlow: 'var(--color-goldModeHoverSoft)', bg: 'var(--color-goldModeBg)', buttonBg: 'var(--color-goldButtonBg)', buttonHover: 'var(--color-goldButtonBgHover)' },
  daily: { icon: '🏆', accent: 'var(--color-gold)', glow: 'var(--color-goldModeHoverStrong)', softGlow: 'var(--color-goldModeHoverSoft)', bg: 'var(--color-goldModeBg)', buttonBg: 'var(--color-goldButtonBg)', buttonHover: 'var(--color-goldButtonBgHover)' },
};

const difficultyTiming = { easy: 700, medium: 1000, hard: 1400 };
const wordFoundPhrases = ['NICE WORD', 'BRILLIANT', 'GREAT CLEAR', 'EXCELLENT', 'BUSSIN 🔥', 'SHARP', 'CLEAN', 'WORD HIT'];
const prestigePhrases = ['GOATED 💎', 'IYKYK 💎', 'PRESTIGE WORD', 'DIFFERENT LEVEL'];

const rivalProfiles = {
  easy: { name: 'BYTE ROOKIE', team: 'The Warmup', copy: 'random moves, real vibes', intro: 'Learning the board, still dangerous.', win: 'BYTE ROOKIE caught a clean W.', loss: 'BYTE ROOKIE got downloaded.', icon: '◇' },
  medium: { name: 'CPU RIVAL', team: 'The Competition', copy: 'lowkey knows the board', intro: 'Balanced reads. Sneaky blocks. Real match energy.', win: 'CPU RIVAL said bet.', loss: 'CPU RIVAL got outplayed.', icon: '◆' },
  hard: { name: 'WORD DEMON', team: 'Threat Engine', copy: 'blocks first, flexes later', intro: 'Hard mode hunts words and blocks your setup.', win: 'WORD DEMON had the board on lock.', loss: 'WORD DEMON caught the L. Main character behavior.', icon: '◈' },
};

const colorChoices = [
  { id: 'phantom', name: 'PHANTOM', color: '#9d4edd', glow: 'rgba(157,78,221,0.7)' },
  { id: 'surge', name: 'SURGE', color: '#00d4ff', glow: 'rgba(0,212,255,0.7)' },
  { id: 'blaze', name: 'BLAZE', color: '#ff006e', glow: 'rgba(255,0,110,0.7)' },
  { id: 'crown', name: 'CROWN', color: '#ffd60a', glow: 'rgba(255,214,10,0.7)' },
  { id: 'venom', name: 'VENOM', color: '#39ff14', glow: 'rgba(57,255,20,0.7)' },
  { id: 'inferno', name: 'INFERNO', color: '#ff3333', glow: 'rgba(255,51,51,0.7)' },
];

const WORD_REVEAL_MS = 1800;

export default function App() {
  const [state, dispatch] = useReducer(reducer, undefined, () => initialState('classic'));
  const [turn, setTurn] = useState('player');
  const [isAnimating, setIsAnimating] = useState(false);
  const audio = useMemo(() => createAudioEngine(), []);
  const chainRef = useRef(Promise.resolve());
  const stateRef = useRef(state);
  const turnRef = useRef(turn);
  const isAnimatingRef = useRef(isAnimating);
  const aiTurnRunningRef = useRef(false);
  const lastPhraseRef = useRef('');
  const [wordPhrase, setWordPhrase] = useState(null);
  const [rankNotice, setRankNotice] = useState(null);
  const [muted, setMuted] = useState(() => localStorage.getItem('neonMuted') === 'true');
  const feverSoundRef = useRef(false);

  useEffect(() => {
    stateRef.current = state;
  }, [state]);

  useEffect(() => {
    const resetScroll = () => window.scrollTo({ top: 0, left: 0, behavior: 'auto' });
    resetScroll();
    const first = window.setTimeout(resetScroll, 0);
    const second = window.setTimeout(resetScroll, 120);
    return () => {
      window.clearTimeout(first);
      window.clearTimeout(second);
    };
  }, [state.screen]);

  useEffect(() => {
    getRank();
  }, []);

  useEffect(() => {
    turnRef.current = turn;
  }, [turn]);

  useEffect(() => {
    isAnimatingRef.current = isAnimating;
  }, [isAnimating]);

  const runScan = useCallback(() => {
    chainRef.current = chainRef.current.then(async () => {
      if (state.vs && state.cascadeDepth >= 2) {
        dispatch({ type: 'SETTLED', message: 'Cascade limit reached' });
        return;
      }
      const matches = findMatches(state.board, { minLength: state.vs ? 2 : 3, vs: state.vs });
      const scoringMatches = state.vs ? getScoringMatchesForState(state, matches) : matches;
      if (!scoringMatches.length) {
        dispatch({ type: 'SETTLED', message: state.mode === 'puzzle' && state.targetWords.length ? 'Find the targets' : 'Ready' });
        return;
      }
      audio.word(scoringMatches[0].word.length);
      audio.crush();
      if (scoringMatches.some((match) => PRESTIGE_WORDS.has(match.word.toUpperCase()))) audio.combo(7);
      if (scoringMatches.length > 1 || state.combo >= 3) audio.combo(state.combo + 1);
      dispatch({ type: 'MARK_CLEARING', matches: scoringMatches });
      await wait(WORD_REVEAL_MS);
      await wait(CLEAR_MS);
      dispatch({ type: 'CLEAR_AND_GRAVITY', matches: scoringMatches });
    });
  }, [audio, state.board, state.cascadeDepth, state.combo, state.mode, state.targetWords.length, state.vs]);

  const waitForState = useCallback((predicate, timeout = 7000) => {
    const startedAt = Date.now();
    return new Promise((resolve) => {
      const tick = () => {
        if (predicate(stateRef.current)) {
          resolve();
          return;
        }
        if (Date.now() - startedAt >= timeout) {
          resolve();
          return;
        }
        window.setTimeout(tick, 30);
      };
      tick();
    });
  }, []);

  const dropTileAsync = useCallback(
    (col, actor) =>
      new Promise((resolve) => {
        dispatch({ type: 'DROP_TILE', col, actor });
        window.setTimeout(resolve, 420);
      }),
    [],
  );

  const runWordScanAsync = useCallback(
    (actor) =>
      waitForState(
        (nextState) =>
          nextState.phase === 'idle' &&
          nextState.lastActor === actor &&
          !nextState.board.flat().some((tile) => tile?.status === 'clearing'),
      ),
    [waitForState],
  );

  const gravityAsync = useCallback(
    () =>
      new Promise((resolve) => {
        window.setTimeout(resolve, 300);
      }),
    [],
  );

  const runVsTurn = useCallback(
    async (actor, col) => {
      setIsAnimating(true);
      await dropTileAsync(col, actor);
      await runWordScanAsync(actor);
      await gravityAsync();
      await runWordScanAsync(actor);
      setIsAnimating(false);
      setTurn(actor === 'player' ? 'ai' : 'player');
    },
    [dropTileAsync, gravityAsync, runWordScanAsync],
  );

  useEffect(() => {
    if (state.screen === 'game' && state.vs && state.aiDifficulty) {
      setTurn('player');
      setIsAnimating(false);
      aiTurnRunningRef.current = false;
    }
  }, [state.aiDifficulty, state.screen, state.vs]);

  useEffect(() => {
    if (turn !== 'ai' && state.currentTurn !== 'ai' && !state.aiThinking) return;
    if (stateRef.current.screen !== 'game' || !stateRef.current.vs || stateRef.current.gameOver) return;
    if (stateRef.current.phase !== 'idle') return;

    const timer = window.setTimeout(async () => {
      if (aiTurnRunningRef.current) return;
      aiTurnRunningRef.current = true;
      try {
        setIsAnimating(true);
        setTurn('ai');
        const col = chooseAiColumn(stateRef.current);
        await dropTileAsync(col, 'ai');
        await runWordScanAsync('ai');
        await gravityAsync();
        await runWordScanAsync('ai');
      } finally {
        setIsAnimating(false);
        setTurn('player');
        aiTurnRunningRef.current = false;
      }
    }, difficultyTiming[stateRef.current.aiDifficulty] ?? 1000);

    return () => window.clearTimeout(timer);
  }, [dropTileAsync, gravityAsync, runWordScanAsync, state.aiThinking, state.currentTurn, state.phase, turn]);

  useEffect(() => {
    if (state.screen !== 'game') return;
    if (state.phase === 'dropping') {
      audio.drop(getTileSoundColor(getDroppedTile(state)));
      if (findDroppedBomb(state.board)) return;
      const cue = getDropCinematicCue(state);
      const timer = window.setTimeout(runScan, cue ? getCinematicDuration(cue.length) : 430);
      return () => window.clearTimeout(timer);
    }
    if (state.phase === 'clearing' && !state.board.flat().some((tile) => tile?.status === 'clearing')) {
      const timer = window.setTimeout(runScan, 430);
      return () => window.clearTimeout(timer);
    }
  }, [audio, runScan, state]);

  useEffect(() => {
    if (state.screen !== 'game' || state.phase !== 'dropping') return;
    const bomb = findDroppedBomb(state.board);
    if (!bomb) return;
    const timer = window.setTimeout(() => {
      audio.bomb();
      dispatch({ type: 'DETONATE_BOMB', row: bomb.row, col: bomb.col });
    }, 430);
    return () => window.clearTimeout(timer);
  }, [audio, state.board, state.phase, state.screen]);

  useEffect(() => {
    if (state.phase === 'idle' && state.screen === 'game') {
      const matches = findMatches(state.board, { minLength: state.vs ? 2 : 3, vs: state.vs });
      const scoringMatches = state.vs ? getScoringMatchesForState(state, matches) : matches;
      if (scoringMatches.length) {
        const timer = window.setTimeout(runScan, 120);
        return () => window.clearTimeout(timer);
      }
      if (state.mode === 'puzzle' && state.targetWords.length === 0) {
        const timer = window.setTimeout(() => dispatch({ type: 'RESULTS', message: 'Puzzle cleared' }), 350);
        return () => window.clearTimeout(timer);
      }
      if (state.mode === 'classic' && state.board[0].some(Boolean)) {
        const timer = window.setTimeout(() => dispatch({ type: 'RESULTS', message: 'Board topped out' }), 350);
        return () => window.clearTimeout(timer);
      }
      if (state.mode === 'puzzle' && state.dropsLeft <= 0) {
        const timer = window.setTimeout(() => dispatch({ type: 'RESULTS', message: 'Drops spent' }), 350);
        return () => window.clearTimeout(timer);
      }
    }
  }, [runScan, state.board, state.dropsLeft, state.mode, state.phase, state.screen, state.targetWords.length, state.vs]);

  useEffect(() => {
    const timer = window.setInterval(() => dispatch({ type: 'RESET_COMBO' }), COMBO_RESET_MS);
    return () => window.clearInterval(timer);
  }, []);

  useEffect(() => {
    if (!state.feedback) return;
    const phrasePool = state.feedback.prestigeWords?.length ? prestigePhrases : wordFoundPhrases;
    const choices = phrasePool.filter((phrase) => phrase !== lastPhraseRef.current);
    const phrase = choices[Math.floor(Math.random() * choices.length)] ?? phrasePool[0];
    lastPhraseRef.current = phrase;
    setWordPhrase(phrase);
    const timer = window.setTimeout(() => dispatch({ type: 'CLEAR_FEEDBACK' }), 3000);
    return () => window.clearTimeout(timer);
  }, [state.feedback]);

  useEffect(() => {
    if (!wordPhrase) return;
    const timer = window.setTimeout(() => setWordPhrase(null), 2800);
    return () => window.clearTimeout(timer);
  }, [wordPhrase]);

  useEffect(() => {
    if (!state.flash) return;
    const timer = window.setTimeout(() => dispatch({ type: 'CLEAR_FLASH', id: state.flash.id }), state.flash.type === 'gameOver' ? 900 : 650);
    return () => window.clearTimeout(timer);
  }, [state.flash]);

  useEffect(() => {
    if (!state.achievementQueue.length) return;
    const timer = window.setTimeout(() => dispatch({ type: 'CLEAR_ACHIEVEMENT' }), 2200);
    return () => window.clearTimeout(timer);
  }, [state.achievementQueue]);

  useEffect(() => {
    if (!state.taunt) return;
    const timer = window.setTimeout(() => dispatch({ type: 'CLEAR_TAUNT' }), 2000);
    return () => window.clearTimeout(timer);
  }, [state.taunt]);

  useEffect(() => {
    if (!state.feverActive) return;
    if (!feverSoundRef.current) {
      audio.fever();
      feverSoundRef.current = true;
    }
    const timer = window.setInterval(() => dispatch({ type: 'FEVER_TICK' }), 250);
    return () => window.clearInterval(timer);
  }, [audio, state.feverActive]);

  useEffect(() => {
    if (!state.feverActive) feverSoundRef.current = false;
  }, [state.feverActive]);

  useEffect(() => {
    if (state.mode !== 'timed' || state.screen !== 'game') return;
    const timer = window.setInterval(() => dispatch({ type: 'TICK' }), 1000);
    return () => window.clearInterval(timer);
  }, [state.mode, state.screen]);

  useEffect(() => {
    if (state.screen !== 'results' || state.mode !== 'daily') return;
    const history = JSON.parse(localStorage.getItem('dailyHistory') || '[]').filter((entry) => entry.date !== state.dailyDate);
    const previousDate = history[0]?.date;
    const yesterday = getOffsetDate(-1);
    const currentStreak = previousDate === yesterday ? Number(localStorage.getItem('dailyStreak') || 0) + 1 : 1;
    const next = [{ date: state.dailyDate, score: state.score, bestWord: bestWord(state.wordsFound).word, prestige: getPrestigeWords(state.wordsFound).length }, ...history].slice(0, 7);
    localStorage.setItem('dailyDate', state.dailyDate);
    localStorage.setItem('dailyScore', String(state.score));
    localStorage.setItem('dailyBestScore', String(state.score));
    localStorage.setItem('dailyStreak', String(currentStreak));
    localStorage.setItem('dailyHistory', JSON.stringify(next));
  }, [state.screen, state.mode, state.dailyDate, state.score, state.wordsFound]);

  useEffect(() => {
    if (state.screen !== 'results' || !state.vs) return;
    const notice = updatePlayerStats(state);
    if (notice) {
      setRankNotice(notice);
      const timer = window.setTimeout(() => setRankNotice(null), notice.type === 'up' ? 2000 : 1500);
      return () => window.clearTimeout(timer);
    }
  }, [state.screen, state.vs, state.score, state.aiScore]);

  useEffect(() => {
    if (state.screen !== 'victory') return;
    audio.victory();
    const timer = window.setTimeout(() => dispatch({ type: 'COMPLETE_VICTORY' }), 3000);
    return () => window.clearTimeout(timer);
  }, [audio, state.screen]);

  useEffect(() => {
    audio.setMuted(muted);
    localStorage.setItem('neonMuted', String(muted));
  }, [audio, muted]);

  const toggleMuted = () => setMuted((current) => !current);

  const drop = (col) => {
    audio.unlock();
    if (stateRef.current.vs) {
      if (turnRef.current !== 'player') return;
      if (isAnimatingRef.current) return;
      runVsTurn('player', col);
      return;
    }
    dispatch({ type: 'DROP_TILE', col });
  };

  return (
    <main className="min-h-screen overflow-hidden bg-[var(--color-background)] text-[var(--color-white)]" style={cssVars}>
      <div className="neon-grid fixed inset-0 animate-grid-pan opacity-30" />
      <div className="fixed inset-0 bg-[radial-gradient(circle_at_50%_0%,var(--color-purpleGlow),transparent_38%),linear-gradient(180deg,transparent,var(--color-background))]" />
      <div className="relative mx-auto flex min-h-screen w-full max-w-md flex-col px-4 py-5 sm:max-w-xl">
        <ScreenFlash flash={state.flash} />
        {rankNotice && <RankNotice notice={rankNotice} />}
        {wordPhrase && <div className="word-found-phrase">{wordPhrase}</div>}
        {state.screen === 'home' && <HomeScreen onStart={(mode) => dispatch({ type: 'START_MODE', mode })} onStartVs={(mode) => dispatch({ type: 'START_VS', mode })} />}
        {state.screen === 'preMatch' && <PreMatchScreen state={state} dispatch={dispatch} />}
        {state.screen === 'difficulty' && <DifficultyScreen state={state} dispatch={dispatch} />}
        {state.screen === 'game' && <GameScreen state={state} onDrop={drop} dispatch={dispatch} turn={turn} isAnimating={isAnimating} muted={muted} onToggleMute={toggleMuted} />}
        {state.screen === 'victory' && <VictoryScreen state={state} />}
        {state.screen === 'results' && <ResultsScreen state={state} dispatch={dispatch} />}
      </div>
    </main>
  );
}

function HomeScreen({ onStart, onStartVs }) {
  const [showPlayerCard, setShowPlayerCard] = useState(false);
  const today = new Date().toISOString().slice(0, 10);
  const dailyDate = localStorage.getItem('dailyDate');
  const dailyBest = localStorage.getItem('dailyScore') ?? localStorage.getItem('dailyBestScore');
  const dailyStreak = Number(localStorage.getItem('dailyStreak') || 0);
  const dailyLocked = dailyDate === today;
  const particles = useMemo(
    () =>
      Array.from({ length: 20 }, (_, index) => ({
        id: index,
        left: `${Math.random() * 100}%`,
        bottom: `${Math.random() * 100}%`,
        delay: `${Math.random() * 8}s`,
        duration: `${7 + Math.random() * 8}s`,
        color: index % 3 === 0 ? 'var(--color-purpleEnd)' : index % 3 === 1 ? 'var(--color-cyan)' : 'var(--color-magenta)',
      })),
    [],
  );

  return (
    <section className="relative flex flex-1 flex-col justify-center gap-7 overflow-hidden pb-8 pt-5">
      <button className="home-player-button" onClick={() => setShowPlayerCard(true)}>▣</button>
      <RankBadge rank={getRank()} className="home-rank-badge" />
      {showPlayerCard && <PlayerCardPanel onClose={() => setShowPlayerCard(false)} />}
      <div className="pointer-events-none absolute inset-0">
        {particles.map((particle) => (
          <span
            key={particle.id}
            className="particle-dot"
            style={{
              left: particle.left,
              bottom: particle.bottom,
              animationDelay: particle.delay,
              animationDuration: particle.duration,
              backgroundColor: particle.color,
            }}
          />
        ))}
      </div>
      <div className="home-logo-shell text-center font-display leading-none animate-logo-flicker">
        <div className="text-6xl text-[var(--color-purpleEnd)] drop-shadow-[0_0_18px_var(--color-purpleGlow)]">NEON</div>
        <div className="text-4xl text-[var(--color-cyan)] drop-shadow-[0_0_14px_var(--color-cyanSoft)]">CONNECT</div>
        <div className="text-5xl text-[var(--color-white)]">WORDS</div>
        <div className="home-tagline">word puzzle · arcade juice · neon purple energy</div>
        <div className="home-howto">Drop letters. Spell words. Clear the board.</div>
      </div>
      <div className="h-px w-full opacity-50 bg-[linear-gradient(90deg,var(--color-purpleEnd),var(--color-cyan),var(--color-purpleEnd))]" />
      <div className="grid grid-cols-1 gap-3">
        {Object.entries(modes).map(([key, mode]) => (
          <div
            key={key}
            className="mode-card group rounded-lg border border-[var(--color-border)] bg-[var(--color-panel)] p-4 text-left shadow-[0_14px_40px_var(--color-shadow)] transition duration-200 active:scale-[.98]"
            data-mode={key}
            style={{ borderLeftColor: modeStyles[key].accent, '--mode-accent': modeStyles[key].accent, '--mode-glow': modeStyles[key].glow, '--mode-soft-glow': modeStyles[key].softGlow, '--mode-bg': modeStyles[key].bg, '--mode-button-bg': modeStyles[key].buttonBg, '--mode-button-hover': modeStyles[key].buttonHover }}
          >
            <button className="w-full text-left" onClick={() => !dailyLocked || key !== 'daily' ? onStart(key) : null}>
              <div className="flex items-center justify-between">
              <span className="flex items-center font-display text-xl text-[var(--color-purpleText)]">
                <span className="mode-icon" style={{ color: modeStyles[key].accent }}>{modeStyles[key].icon}</span>
                {key === 'daily' ? 'DAILY ARENA' : mode.title}
              </span>
              <span className="play-dot group-active:translate-x-1">{key === 'daily' && dailyLocked ? '—' : '▶'}</span>
              </div>
              <p className="mt-1 font-ui text-lg text-[var(--color-white)]/75">{key === 'daily' && dailyLocked ? `Come back tomorrow · ${dailyBest ?? 0}` : mode.label}</p>
              {key === 'daily' && <p className="daily-streak-line">{dailyStreak ? `${dailyStreak} day streak · one shot only` : 'Start your daily streak'}</p>}
            </button>
            {(key === 'classic' || key === 'timed') && (
              <button
                className="vs-computer-button mt-3"
                onClick={() => onStartVs(key)}
              >
                VS COMPUTER
              </button>
            )}
          </div>
        ))}
      </div>
      <StudioTag className="mt-auto pb-1" />
    </section>
  );
}

function PlayerCardPanel({ onClose }) {
  const rank = getRank();
  const stats = getPlayerStats();
  const winRate = stats.matches ? Math.round((stats.wins / stats.matches) * 100) : 0;
  const quote = winRate > 60 ? 'bussin every session no cap' : winRate >= 40 ? 'lowkey improving, keep going' : 'the glow up era starts now';
  const prestigeWords = stats.prestigeWords ?? [];
  const nextPrestige = [...PRESTIGE_WORDS].find((word) => !prestigeWords.includes(word)) ?? 'COMPLETE';
  return (
    <div className="player-card-panel">
      <button className="absolute right-4 top-4 font-mono text-[var(--color-cyan)]" onClick={onClose}>CLOSE</button>
      <div className="font-display text-3xl text-[var(--color-white)]">{localStorage.getItem('playerName') || 'PLAYER'}</div>
      <div className="font-mono text-sm text-[var(--color-watermark)]">{localStorage.getItem('teamName') || 'PHANTOM FC'}</div>
      <RankBadge rank={rank} className="my-3" />
      <div className="mx-auto my-4 h-[60px] w-[60px] rounded-full bg-[var(--color-purpleEnd)] shadow-[0_0_24px_var(--color-purpleModeHoverStrong)]" />
      <div className="grid grid-cols-2 gap-3">
        <PlayerStat label="MATCHES PLAYED" value={stats.matches} />
        <PlayerStat label="WINS" value={stats.wins} />
        <PlayerStat label="LOSSES" value={stats.losses} />
        <PlayerStat label="WIN RATE" value={`${winRate}%`} />
        <PlayerStat label="BEST WORD" value={stats.bestWord || 'NONE'} />
        <PlayerStat label="LONGEST STREAK" value={stats.longestStreak} />
      </div>
      <div className="prestige-collection">
        <div className="font-display text-sm text-[var(--color-gold)]">PRESTIGE WORDS FOUND · {prestigeWords.length}/{PRESTIGE_WORDS.size}</div>
        <div className="font-mono text-[10px] uppercase tracking-[2px] text-[var(--color-cyan)]">Next chase: {nextPrestige}</div>
        <div className="prestige-badges">
          {prestigeWords.length ? prestigeWords.map((word) => <span key={word}>{word}</span>) : <span>0</span>}
        </div>
      </div>
      <p className="mt-5 text-center font-mono text-[11px] italic text-[var(--color-watermark)]">{quote}</p>
    </div>
  );
}

function PlayerStat({ label, value }) {
  return (
    <div className="rounded-md border border-[var(--color-border)] bg-[var(--color-background)] p-3">
      <div className="font-mono text-[10px] uppercase text-[var(--color-watermark)]">{label}</div>
      <div className="font-display text-[22px] font-bold text-[var(--color-white)]">{value}</div>
    </div>
  );
}

function ScreenFlash({ flash }) {
  if (!flash) return null;
  return <div className={`screen-flash flash-${flash.type}`} aria-hidden="true" />;
}

function AchievementBanner({ achievement }) {
  return <div className={`achievement-banner ${achievement ? 'achievement-visible' : ''}`}>{achievement?.label}</div>;
}

function RankNotice({ notice }) {
  const isUp = notice.type === 'up';
  return (
    <div className={`rank-notice rank-notice-${notice.type}`} style={{ '--rank-notice-color': notice.to?.color ?? '#ff3333' }}>
      <div className="rank-flash" />
      <div className="rank-notice-card">
        <div>{isUp ? "RANK UP — YOU'RE ON A DIFF LEVEL" : 'take the L, come back stronger'}</div>
        {isUp && <span>{notice.from.name} → {notice.to.name}</span>}
      </div>
    </div>
  );
}

function PreMatchScreen({ state, dispatch }) {
  const [playerName, setPlayerName] = useState(state.vsSetup.playerName);
  const [teamName, setTeamName] = useState(state.vsSetup.teamName);
  const [pendingColor, setPendingColor] = useState(state.vsSetup.playerColor);
  const [selectedDifficulty, setSelectedDifficulty] = useState(state.aiDifficulty ?? 'medium');
  const rank = getRank();
  const selectedColor = colorChoices.find((choice) => choice.color === state.vsSetup.playerColor) ?? colorChoices[0];
  const aiColor = colorChoices.find((choice) => choice.color === state.vsSetup.aiColor) ?? colorChoices[5];
  const rival = getRivalProfile(selectedDifficulty);

  if (state.vsSetup.step === 1) {
    return (
      <section className="flex flex-1 flex-col justify-center gap-4">
        <h1 className="font-display text-[28px] text-[var(--color-scorePurple)]">WHO ARE YOU?</h1>
        <input className="setup-input" maxLength={12} placeholder="Enter your name" value={playerName} onChange={(event) => setPlayerName(event.target.value)} />
        <input className="setup-input" maxLength={16} placeholder="Name your squad" value={teamName} onChange={(event) => setTeamName(event.target.value)} />
        <p className="font-mono text-[11px] tracking-[3px] text-[var(--color-watermark)]">lowkey this is your era</p>
        <button
          className="rounded-lg bg-[var(--color-purpleEnd)] py-4 font-display text-sm text-[var(--color-white)] shadow-[0_0_18px_var(--color-purpleGlow)]"
          onClick={() => {
            localStorage.setItem('playerName', playerName || 'PLAYER');
            localStorage.setItem('teamName', teamName || 'PHANTOM FC');
            dispatch({ type: 'SET_VS_IDENTITY', payload: { playerName: playerName || 'PLAYER', teamName: teamName || 'PHANTOM FC' } });
          }}
        >
          LET'S RUN IT
        </button>
      </section>
    );
  }

  if (state.vsSetup.step === 2) {
    return (
      <section className="flex flex-1 flex-col justify-center gap-5">
        <h1 className="font-display text-[28px] text-[var(--color-scorePurple)]">PICK YOUR COLOR, NO CAP</h1>
        <div className="grid grid-cols-2 gap-4">
          {colorChoices.map((choice) => (
            <button
              key={choice.id}
              className={`color-orb-button ${choice.color === pendingColor ? 'color-orb-selected' : ''}`}
              style={{ '--orb-color': choice.color, '--orb-glow': choice.glow }}
              onClick={() => setPendingColor(choice.color)}
            >
              <span className="color-orb" />
              <span>{choice.name}</span>
              {choice.color === pendingColor && <span className="color-check">✓</span>}
            </button>
          ))}
        </div>
        <button
          className="rounded-lg bg-[var(--color-purpleEnd)] py-4 font-display text-sm text-[var(--color-white)] shadow-[0_0_18px_var(--color-purpleGlow)]"
          onClick={() => dispatch({ type: 'SET_VS_COLOR', playerColor: pendingColor, aiColor: pickContrastColor(pendingColor) })}
        >
          LOCK IT IN
        </button>
      </section>
    );
  }

  const difficultyChoices = [
    ['easy', 'EASY', 'var(--color-green)'],
    ['medium', 'MEDIUM', 'var(--color-amber)'],
    ['hard', 'HARD', 'var(--color-aiRed)'],
  ];
  return (
    <section className="flex flex-1 flex-col justify-center gap-5">
      <div className="vs-preview-card grid grid-cols-[1fr_auto_1fr] items-center gap-3 rounded-lg border border-[var(--color-border)] bg-[var(--color-panel)] p-4">
        <VsPreviewCard name={state.vsSetup.playerName} team={state.vsSetup.teamName} color={selectedColor} rank={rank} copy="it's giving main character energy" />
        <div className="font-display text-4xl text-[var(--color-scorePurple)] [text-shadow:0_0_18px_var(--color-purpleGlow)]">VS</div>
        <VsPreviewCard name={rival.name} team={rival.team} color={aiColor} rank={null} copy={rival.copy} />
      </div>
      <div className="rival-intro-card">
        <span>{rival.icon}</span>
        <p>{rival.intro}</p>
      </div>
      <div className="grid grid-cols-3 gap-2">
        {difficultyChoices.map(([id, label, accent]) => (
          <button
            key={id}
            className={`difficulty-chip ${selectedDifficulty === id ? 'difficulty-chip-selected' : ''}`}
            style={{ '--difficulty-accent': accent }}
            onClick={() => setSelectedDifficulty(id)}
          >
            {label}
          </button>
        ))}
      </div>
      <button className="rounded-lg bg-[var(--color-gold)] py-4 font-display text-sm text-[var(--color-background)] shadow-[0_0_18px_var(--color-gold)]" onClick={() => dispatch({ type: 'CHOOSE_DIFFICULTY', difficulty: selectedDifficulty })}>
        NO CAP LET'S GO
      </button>
      <StudioTag className="pt-2" />
    </section>
  );
}

function VsPreviewCard({ name, team, color, rank, copy }) {
  return (
    <div className="text-center">
      <div className="font-display text-[20px] text-[var(--color-white)]">{name}</div>
      <div className="font-mono text-[12px] text-[var(--color-watermark)]">{team}</div>
      {rank && <RankBadge rank={rank} />}
      <div className="mx-auto my-3 h-10 w-10 rounded-full" style={{ background: color.color, boxShadow: `0 0 20px ${color.glow}` }} />
      <div className="font-mono text-[10px] italic text-[var(--color-watermark)]">{copy}</div>
    </div>
  );
}

function getRivalProfile(difficulty) {
  return rivalProfiles[difficulty] ?? rivalProfiles.medium;
}

function DifficultyScreen({ state, dispatch }) {
  const choices = [
    ['easy', 'EASY', 'Random non-full column', 'var(--color-green)'],
    ['medium', 'MEDIUM', 'Best 2-letter setup, 30% random', 'var(--color-amber)'],
    ['hard', 'HARD', 'Highest scoring drop and blocks threats', 'var(--color-aiRed)'],
  ];
  return (
    <section className="flex flex-1 flex-col justify-center gap-4">
      <div className="text-center">
        <div className="font-mono text-sm uppercase tracking-[3px] text-[var(--color-watermark)]">{state.mode} VS COMPUTER</div>
        <h1 className="mt-2 font-display text-4xl text-[var(--color-gold)]">DIFFICULTY</h1>
      </div>
      {choices.map(([id, title, copy, accent]) => (
        <button
          key={id}
          className="rounded-lg border border-[var(--color-border)] border-l-4 bg-[var(--color-panel)] p-4 text-left shadow-[0_14px_40px_var(--color-shadow)]"
          style={{ borderLeftColor: accent }}
          onClick={() => dispatch({ type: 'CHOOSE_DIFFICULTY', difficulty: id })}
        >
          <div className="font-display text-xl" style={{ color: accent }}>
            {title}
          </div>
          <p className="font-ui text-lg text-[var(--color-white)]/75">{copy}</p>
        </button>
      ))}
      <button className="rounded-lg border border-[var(--color-border)] bg-[var(--color-panel)] py-3 font-display text-sm text-[var(--color-cyan)]" onClick={() => dispatch({ type: 'SHOW_HOME' })}>
        BACK
      </button>
    </section>
  );
}

function GameScreen({ state, onDrop, dispatch, turn, isAnimating, muted, onToggleMute }) {
  const [hoverCol, setHoverCol] = useState(null);
  const [queueDrag, setQueueDrag] = useState(false);
  const queueDragTimerRef = useRef(null);
  const timePercent = state.mode === 'timed' ? Math.max(0, (state.timeLeft / TIMED_SECONDS) * 100) : 100;
  const comboFill = Math.min(100, (state.combo / 9) * 100);
  const hotZonesVisible = !state.vs && state.mode !== 'daily' && state.board.slice(3).some((row) => row.some(Boolean));
  const rowCount = state.board.length;
  const cinematicCue = getDropCinematicCue(state);
  const impactCells = new Set(state.clearingImpact?.cells.map((cell) => `${cell.row},${cell.col}`) ?? []);
  const impactTier = getImpactTier(state.clearingImpact?.length ?? 0);
  const chainTrails = getConnectChainTrails(state);
  const feverLeft = state.feverActive ? Math.max(0, 100 - ((Date.now() - state.feverStartedAt) / 10000) * 100) : 0;
  const feedbackStyle = state.feedback
    ? {
        left: `${((state.feedback.anchor.col + 0.5) / COLS) * 100}%`,
        top: `${((state.feedback.anchor.row + 0.5) / rowCount) * 100}%`,
      }
    : {};
  const columnInputDisabled = state.phase !== 'idle' || Boolean(state.swapMode) || (state.vs && (turn !== 'player' || isAnimating));
  const handleQueueDrop = (col) => {
    if (columnInputDisabled) return;
    onDrop(col);
  };

  useEffect(() => {
    if (!queueDrag) return;
    const clearQueueDrag = () => {
      if (queueDragTimerRef.current) window.clearTimeout(queueDragTimerRef.current);
      queueDragTimerRef.current = window.setTimeout(() => {
        setQueueDrag(false);
        setHoverCol(null);
        queueDragTimerRef.current = null;
      }, 0);
    };
    window.addEventListener('dragend', clearQueueDrag);
    window.addEventListener('drop', clearQueueDrag);
    window.addEventListener('mouseup', clearQueueDrag);
    window.addEventListener('touchend', clearQueueDrag);
    window.addEventListener('touchcancel', clearQueueDrag);
    window.addEventListener('blur', clearQueueDrag);
    document.addEventListener('visibilitychange', clearQueueDrag);
    return () => {
      window.removeEventListener('dragend', clearQueueDrag);
      window.removeEventListener('drop', clearQueueDrag);
      window.removeEventListener('mouseup', clearQueueDrag);
      window.removeEventListener('touchend', clearQueueDrag);
      window.removeEventListener('touchcancel', clearQueueDrag);
      window.removeEventListener('blur', clearQueueDrag);
      document.removeEventListener('visibilitychange', clearQueueDrag);
      if (queueDragTimerRef.current) {
        window.clearTimeout(queueDragTimerRef.current);
        queueDragTimerRef.current = null;
      }
    };
  }, [queueDrag]);

  return (
    <section className="game-screen-stack flex flex-1 flex-col gap-3">
      <button className="mute-button" onClick={onToggleMute} aria-label={muted ? 'Unmute sound' : 'Mute sound'}>
        {muted ? 'SOUND OFF' : 'SOUND ON'}
      </button>
      <Hud state={state} dispatch={dispatch} />
      {state.vs && <VsScoreboard state={state} />}
      <AchievementBanner achievement={state.achievementQueue[0]} />
      {state.mode === 'timed' && (
        <div className="h-2 overflow-hidden rounded-full bg-[var(--color-panel)]">
          <div className="h-full bg-[var(--color-cyan)] transition-all duration-500" style={{ width: `${timePercent}%` }} />
        </div>
      )}
      <HighlightMoment feedback={state.feedback} combo={state.combo} />
      <div className="board-panel rounded-lg border bg-[var(--color-panel)] p-3">
        {state.feverActive && (
          <div className="mb-2 text-center">
            <div className="animate-combo-pulse font-display text-2xl text-[var(--color-gold)] [text-shadow:0_0_14px_var(--color-gold)]">FEVER</div>
            <div className="mt-1 h-2 overflow-hidden rounded-full bg-[var(--color-background)]">
              <div className="h-full bg-[var(--color-gold)] transition-all duration-200" style={{ width: `${feverLeft}%` }} />
            </div>
          </div>
        )}
        <PreviewQueue
          tiles={state.nextTiles ?? []}
          board={state.board}
          disabled={columnInputDisabled}
          dragging={queueDrag}
          setDragging={setQueueDrag}
          setHoverCol={setHoverCol}
          onDropColumn={handleQueueDrop}
        />
        <GoalStrip state={state} />
        <div className="word-found-strip">
          {state.feedback && <div className="animate-word-float word-found-text">{state.feedback.words.join(' + ')}</div>}
        </div>
        <ColumnTaps board={state.board} onDrop={onDrop} disabled={columnInputDisabled} setHoverCol={setHoverCol} dragActive={queueDrag} onQueueDrop={handleQueueDrop} />
        <div
          className={`neon-board relative grid grid-cols-7 gap-1.5 rounded-md border bg-[var(--color-background)] p-2 ${state.feverActive ? 'fever-board' : ''}`}
          style={{ aspectRatio: `7 / ${rowCount}`, gridTemplateRows: `repeat(${rowCount}, minmax(0, 1fr))` }}
        >
          <div className="board-depth-overlay" />
          <div className="board-corner board-corner-tl" />
          <div className="board-corner board-corner-tr" />
          <div className="board-corner board-corner-bl" />
          <div className="board-corner board-corner-br" />
          {chainTrails.length > 0 && <ChainTrailOverlay trails={chainTrails} rowCount={rowCount} />}
          {hoverCol !== null && state.phase === 'idle' && !state.swapMode && <div className="column-highlight" style={{ left: `calc(${(hoverCol / COLS) * 100}% + 0.5rem)`, width: `calc(${100 / COLS}% - 0.375rem)` }} />}
          {state.board.map((row, rowIndex) =>
            row.map((tile, colIndex) => (
              <Cell
                key={`${rowIndex}-${colIndex}`}
                tile={tile}
                row={rowIndex}
                col={colIndex}
                active={state.swapMode?.first?.row === rowIndex && state.swapMode?.first?.col === colIndex}
                columnHot={hoverCol === colIndex && state.phase === 'idle' && !state.swapMode}
                hotZone={Boolean(tile) && hotZonesVisible && state.hotZones.some((zone) => zone.row === rowIndex && zone.col === colIndex)}
                hotBurst={state.feedback?.hotCells?.some((cell) => cell.row === rowIndex && cell.col === colIndex)}
                cinematicCue={cinematicCue}
                impactTier={impactCells.has(`${rowIndex},${colIndex}`) ? impactTier : null}
                fever={state.feverActive}
                onClick={() => {
                  if (tile?.special === 'swap') dispatch({ type: 'USE_SWAP_TILE' });
                  else if (state.swapMode) dispatch({ type: 'PICK_SWAP', row: rowIndex, col: colIndex });
                }}
              />
            )),
          )}
          {state.feedback && (
            <div className="pointer-events-none absolute z-20 -translate-x-1/2 -translate-y-1/2 text-center" style={feedbackStyle}>
              <div className="animate-score-rise score-rise-text">+{state.feedback.points}</div>
            </div>
          )}
        </div>
      </div>
      <WordsSpelledBox wordsFound={state.wordsFound} feedback={state.feedback} />
      <ComboPanel combo={state.combo} streak={state.streak} fill={comboFill} pendingSpecial={state.pendingSpecial} />
      {state.vs && state.objective && <ObjectivePanel state={state} />}
      <StatusPanel state={state} />
      {state.vs && state.aiThinking && <div className="text-center font-mono text-xs tracking-[3px] text-[var(--color-watermark)]">AI THINKING...</div>}
      {state.vs && <QuickTaunts state={state} dispatch={dispatch} />}
      <button
        className="mt-auto rounded-lg border border-[var(--color-border)] bg-[var(--color-panel)] py-3 font-display text-sm text-[var(--color-cyan)]"
        onClick={() => dispatch({ type: 'SHOW_HOME' })}
      >
        HOME
      </button>
    </section>
  );
}

function Hud({ state, dispatch }) {
  const score = Number.isFinite(Number(state.score)) ? Number(state.score) : 0;
  const streak = Number.isFinite(Number(state.streak)) ? Number(state.streak) : 0;
  const wordsSpelled = Array.isArray(state.wordsFound) ? state.wordsFound.length : 0;
  const scoreText = String(Math.trunc(score));
  const streakText = String(Math.trunc(streak));
  return (
    <header className="hud-panel grid grid-cols-4 items-center gap-2 rounded-lg border border-[var(--color-border)] bg-[var(--color-panel)] p-3">
      <div>
        <div className="hud-label">{state.vs ? 'YOU' : 'SCORE'}</div>
        <div className="hud-score font-display font-black leading-none text-[var(--color-scorePurple)]">{scoreText}</div>
        {state.vs && <div className="font-mono text-[10px] text-[var(--color-aiRed)]">AI {state.aiScore}</div>}
        {state.vs && (
          <button className={`power-drop-button ${state.powerDropUsed.player ? 'power-drop-used' : ''}`} disabled={state.powerDropUsed.player || state.currentTurn !== 'player'} onClick={() => dispatch({ type: 'ARM_POWER_DROP' })}>
            {state.powerDropUsed.player ? 'USED' : 'POWER DROP ⚡'}
          </button>
        )}
      </div>
      <div className="text-center">
        <div className="hud-label">STREAK</div>
        <div className="hud-streak font-display font-black leading-none text-[var(--color-gold)]">{streakText}</div>
      </div>
      <div className="text-center">
        <div className="hud-label">WORDS</div>
        <div className="hud-words font-display font-black leading-none">{wordsSpelled}</div>
      </div>
      <div className="text-right">
        <div className="hud-label">COMBO</div>
        <div
          className={`hud-combo font-display font-black leading-none ${
            state.combo >= 2 ? 'animate-combo-pulse text-[var(--color-comboMagenta)]' : 'text-[var(--color-cyan)]'
          } ${state.combo >= 5 ? 'animate-combo-shake' : ''}`}
        >
          x{state.combo}
        </div>
      </div>
    </header>
  );
}

function VsScoreboard({ state }) {
  const playerName = state.vsSetup.playerName || 'PLAYER';
  const rival = getRivalProfile(state.aiDifficulty);
  return (
    <div className="vs-scoreboard">
      <div className="vs-score-side" style={{ color: state.vsSetup.playerColor }}>
        <div>{playerName}</div>
        <strong>{state.score}</strong>
        <RoundDots wins={state.playerRounds} color={state.vsSetup.playerColor} />
      </div>
      <div className="vs-round-score">
        <span>{state.playerRounds}</span>
        <span>—</span>
        <span>{state.aiRounds}</span>
      </div>
      <div className="vs-score-side text-right" style={{ color: state.vsSetup.aiColor }}>
        <div>{rival.name}</div>
        <strong>{state.aiScore}</strong>
        <RoundDots wins={state.aiRounds} color={state.vsSetup.aiColor} align="end" />
      </div>
    </div>
  );
}

function RoundDots({ wins, color, align = 'start' }) {
  return (
    <div className={`round-dots ${align === 'end' ? 'justify-end' : ''}`}>
      {[0, 1].map((index) => (
        <span key={index} className="round-dot" style={{ background: index < wins ? color : 'transparent', borderColor: color }} />
      ))}
    </div>
  );
}

function ObjectivePanel({ state }) {
  return (
    <div className="objective-panel">
      <span>{state.objective.name}</span>
      <span>{state.objective.requirement}</span>
      <span>{state.objectiveProgress}/{state.objective.goal}</span>
    </div>
  );
}

function HighlightMoment({ feedback, combo }) {
  if (!feedback) return null;
  const hasPrestige = feedback.prestigeWords?.length > 0;
  const title = hasPrestige ? 'PRESTIGE WORD' : feedback.intersection ? 'CROSSWORD COMBO' : combo >= 5 ? 'COMBO HEATER' : 'WORD HIT';
  const copy = hasPrestige ? feedback.prestigeWords.join(' · ') : feedback.words.join(' · ');
  return (
    <div className={`highlight-moment ${hasPrestige ? 'highlight-prestige' : ''}`}>
      <span>{title}</span>
      <strong>{copy}</strong>
      <em>+{feedback.points}</em>
    </div>
  );
}

function QuickTaunts({ state, dispatch }) {
  const [open, setOpen] = useState(false);
  const taunts = ["LET'S GO 🔥", 'TOO EASY 😤', 'NICE WORD 🤝', "IT'S GIVING L 💀"];
  return (
    <>
      {state.taunt && <div className="taunt-flash" style={{ color: state.vsSetup.playerColor }}>{state.taunt}</div>}
      <div className="taunt-tray">
        <button className="taunt-toggle" onClick={() => setOpen(!open)}>TAUNT</button>
        {open && taunts.map((taunt) => (
          <button key={taunt} className="taunt-option" onClick={() => dispatch({ type: 'SEND_TAUNT', text: taunt })}>{taunt}</button>
        ))}
      </div>
    </>
  );
}

function PreviewQueue({ tiles, board, disabled, dragging, setDragging, setHoverCol, onDropColumn }) {
  const dragActiveRef = useRef(false);
  const [dragPoint, setDragPoint] = useState(null);
  const clearDrag = () => {
    dragActiveRef.current = false;
    setDragging(false);
    setHoverCol(null);
    setDragPoint(null);
  };
  const columnHasSpace = (col) => Number.isInteger(col) && col >= 0 && col < COLS && board.some((row) => !row[col]);
  const findDropColumn = (x, y) => {
    const target = document.elementFromPoint(x, y);
    const button = target?.closest?.('[data-drop-col]');
    if (button && !button.disabled) {
      const col = Number(button.dataset.dropCol);
      return columnHasSpace(col) ? col : null;
    }
    const boardElement = document.querySelector('.neon-board');
    const dropRowElement = document.querySelector('.drop-zone-row');
    const boardRect = boardElement?.getBoundingClientRect();
    const dropRect = dropRowElement?.getBoundingClientRect();
    const rect = boardRect && dropRect
      ? {
          left: Math.min(boardRect.left, dropRect.left),
          right: Math.max(boardRect.right, dropRect.right),
          top: Math.min(boardRect.top, dropRect.top) - 26,
          bottom: boardRect.bottom + 20,
          width: Math.max(boardRect.right, dropRect.right) - Math.min(boardRect.left, dropRect.left),
        }
      : boardRect;
    if (!rect || x < rect.left || x > rect.right || y < rect.top || y > rect.bottom) return null;
    const col = Math.max(0, Math.min(COLS - 1, Math.floor(((x - rect.left) / rect.width) * COLS)));
    return columnHasSpace(col) ? col : null;
  };
  const startDrag = (x, y) => {
    dragActiveRef.current = true;
    setDragging(true);
    setDragPoint({ x, y });
    setHoverCol(findDropColumn(x, y));
  };
  const moveDrag = (x, y) => {
    if (!dragActiveRef.current) return;
    setDragPoint({ x, y });
    setHoverCol(findDropColumn(x, y));
  };
  const finishDrag = (x, y) => {
    if (!dragActiveRef.current) return;
    const col = findDropColumn(x, y);
    clearDrag();
    if (Number.isInteger(col)) onDropColumn(col);
  };
  const handleTouchMove = (event) => {
    if (!dragActiveRef.current) return;
    event.preventDefault();
    const touch = event.touches[0];
    moveDrag(touch.clientX, touch.clientY);
  };
  const handleTouchEnd = (event) => {
    if (!dragActiveRef.current) return;
    const touch = event.changedTouches[0];
    finishDrag(touch.clientX, touch.clientY);
  };
  const handlePointerMove = (event) => moveDrag(event.clientX, event.clientY);
  const handlePointerUp = (event) => finishDrag(event.clientX, event.clientY);

  useEffect(() => {
    if (!dragging) return;
    window.addEventListener('pointermove', handlePointerMove);
    window.addEventListener('pointerup', handlePointerUp);
    window.addEventListener('pointercancel', clearDrag);
    return () => {
      window.removeEventListener('pointermove', handlePointerMove);
      window.removeEventListener('pointerup', handlePointerUp);
      window.removeEventListener('pointercancel', clearDrag);
    };
  }, [dragging]);

  return (
    <div className="next-queue">
      <span>NEXT</span>
      <div className="next-queue-tiles">
        {tiles.map((tile, index) => (
          <div
            key={`${tile.id}-${index}`}
            className={`next-tile ${tone[tile.variant] ?? 'tile-purple'} ${index === 0 && !disabled ? 'next-tile-active' : ''} ${index === 0 && dragging ? 'next-tile-dragging' : ''}`}
            draggable={false}
            onDragStart={(event) => {
              if (index !== 0 || disabled) return;
              event.dataTransfer.effectAllowed = 'move';
              event.dataTransfer.setData('text/plain', tile.id);
              setDragging(true);
            }}
            onDragEnd={clearDrag}
            onPointerDown={(event) => {
              if (index !== 0 || disabled || event.pointerType === 'touch') return;
              event.preventDefault();
              event.currentTarget.setPointerCapture?.(event.pointerId);
              startDrag(event.clientX, event.clientY);
            }}
            onTouchStart={(event) => {
              if (index !== 0 || disabled) return;
              event.preventDefault();
              const touch = event.touches[0];
              startDrag(touch.clientX, touch.clientY);
            }}
            onTouchMove={handleTouchMove}
            onTouchEnd={handleTouchEnd}
            onTouchCancel={clearDrag}
          >
            <span>{tile.letter}</span>
            {index === 0 && !disabled && <small>DRAG</small>}
          </div>
        ))}
      </div>
      {dragPoint && tiles[0] && (
        <div
          className={`next-tile next-tile-drag-preview ${tone[tiles[0].variant] ?? 'tile-purple'}`}
          style={{ left: `${dragPoint.x}px`, top: `${dragPoint.y}px` }}
          aria-hidden="true"
        >
          <span>{tiles[0].letter}</span>
        </div>
      )}
    </div>
  );
}

function ColumnTaps({ board, onDrop, disabled, setHoverCol, dragActive, onQueueDrop }) {
  return (
    <div className="drop-zone-row mb-2 grid grid-cols-7 gap-1.5">
      {Array.from({ length: COLS }).map((_, col) => {
        const emptyRows = board.reduce((count, row) => count + (row[col] ? 0 : 1), 0);
        const full = emptyRows === 0;
        const dangerClass = full ? 'drop-button-full' : emptyRows === 1 ? 'drop-button-danger' : emptyRows === 2 ? 'drop-button-warning' : '';
        return (
          <button
            type="button"
            key={col}
            className={`drop-button ${dangerClass} ${dragActive && !disabled && !full ? 'drop-button-drag-target' : ''}`}
            data-drop-col={col}
            disabled={disabled || full}
            onDragOver={(event) => {
              if (disabled || full || !dragActive) return;
              event.preventDefault();
              event.dataTransfer.dropEffect = 'move';
              setHoverCol(col);
            }}
            onDrop={(event) => {
              if (disabled || full || !dragActive) return;
              event.preventDefault();
              setHoverCol(null);
              onQueueDrop(col);
            }}
            onMouseEnter={() => setHoverCol(col)}
            onMouseLeave={() => setHoverCol(null)}
            onFocus={() => setHoverCol(col)}
            onBlur={() => setHoverCol(null)}
            onClick={() => onDrop(col)}
          >
            {!full && '↓'}
          </button>
        );
      })}
    </div>
  );
}

function ChainTrailOverlay({ trails, rowCount }) {
  return (
    <svg className="chain-trail-overlay" viewBox={`0 0 ${COLS} ${rowCount}`} preserveAspectRatio="none" aria-hidden="true">
      {trails.map((trail, index) => (
        <line
          key={`${trail.owner}-${index}-${trail.x1}-${trail.y1}-${trail.x2}-${trail.y2}`}
          className="chain-trail-line"
          x1={trail.x1}
          y1={trail.y1}
          x2={trail.x2}
          y2={trail.y2}
          style={{ '--chain-color': trail.color }}
        />
      ))}
    </svg>
  );
}

function Cell({ tile, row, col, active, columnHot, hotZone, hotBurst, cinematicCue, impactTier, fever, onClick }) {
  const cellKey = `${row},${col}`;
  const isCinematicWordCell = cinematicCue?.cells.has(cellKey);
  const isCinematicDrop = tile?.justDropped && cinematicCue?.dropKey === cellKey;
  const cinematicTier = isCinematicDrop ? getImpactTier(cinematicCue.length) : null;
  return (
    <button
      type="button"
      className={`relative min-w-0 rounded-md border border-[var(--color-emptyCellBorder)] bg-[var(--color-emptyCell)] transition ${
        columnHot ? 'bg-[var(--color-purpleGlow)] shadow-[inset_0_0_18px_var(--color-purpleGlow)]' : ''
      } ${hotZone ? 'hot-zone-cell' : ''} ${hotBurst ? 'hot-zone-burst' : ''} ${isCinematicWordCell && !isCinematicDrop ? 'anticipation-pulse' : ''} ${active ? 'ring-2 ring-[var(--color-gold)]' : ''}`}
      onClick={onClick}
      aria-disabled={!tile}
    >
      {tile && (
        <div
          className={[
            'absolute inset-0 grid place-items-center rounded-md border border-[var(--color-white)]/10 font-display text-xl leading-none sm:text-2xl',
            tile.status === 'clearing' ? 'tile-gold animate-tile-clear' : tone[tile.variant],
            fever ? 'tile-fever' : '',
            tile.justDropped ? 'animate-tile-drop tile-land' : '',
            isCinematicDrop ? `cinematic-drop cinematic-${cinematicTier}` : '',
            impactTier ? `word-impact word-impact-${impactTier}` : '',
            tile.special ? 'text-[var(--color-gold)]' : '',
          ].join(' ')}
        >
          {isCinematicDrop && [0.35, 0.28, 0.2, 0.13, 0.06].map((opacity, index) => (
            <span key={opacity} className="cinematic-ghost" style={{ '--ghost-opacity': opacity, '--ghost-offset': `${(index + 1) * 8}px` }} />
          ))}
          <span className="tile-letter">{tile.letter}</span>
          <span className="absolute right-1 top-1 font-mono text-[10px] opacity-85">{tile.value}</span>
        </div>
      )}
    </button>
  );
}

function ComboPanel({ combo, streak, fill, pendingSpecial }) {
  const fillColor = getComboColor(combo);
  const fillGradient = getComboGradient(combo);
  const fillGlow = getComboGlow(combo);
  return (
    <aside className="combo-panel rounded-lg border border-[var(--color-border)] bg-[var(--color-panel)] p-3" style={{ '--combo-color': fillColor, '--combo-gradient': fillGradient, '--combo-glow': fillGlow }}>
      <div className="mb-2 flex items-center justify-between">
        <span className="combo-label">COMBO METER</span>
        <span className="font-mono text-sm text-[var(--color-gold)]">{pendingSpecial ? `${pendingSpecial.toUpperCase()} READY` : `${streak} WORD STREAK`}</span>
      </div>
      <div className="combo-track overflow-hidden rounded-full bg-[var(--color-background)]">
        <div
          className="combo-fill h-full rounded-full transition-all duration-300"
          style={{ width: `${fill}%` }}
        />
      </div>
      <div className="mt-2 font-mono text-xs text-[var(--color-white)]/70">x{combo} resets after 5s idle</div>
    </aside>
  );
}

function GoalStrip({ state }) {
  const wordsSpelled = Array.isArray(state.wordsFound) ? state.wordsFound.length : 0;
  const best = getStoredBestScore();
  const bestFourPlus = bestWord(state.wordsFound).word;
  const hasFourPlus = bestFourPlus !== 'NONE' && bestFourPlus.length >= 4;
  const goals = [
    { label: 'Spell 5 words', value: `${Math.min(wordsSpelled, 5)}/5`, complete: wordsSpelled >= 5 },
    { label: 'Find a 4+ word', value: hasFourPlus ? bestFourPlus : '—', complete: hasFourPlus },
    { label: best > 0 ? 'Beat best score' : 'Set first best', value: best > 0 ? `${state.score}/${best}` : `${state.score}`, complete: best > 0 ? state.score > best : state.score > 0 },
  ];

  return (
    <div className="goal-strip" aria-label="Current goals">
      {goals.map((goal) => (
        <div className={`goal-chip ${goal.complete ? 'goal-chip-complete' : ''}`} key={goal.label}>
          <span>{goal.label}</span>
          <strong>{goal.value}</strong>
        </div>
      ))}
    </div>
  );
}

function WordsSpelledBox({ wordsFound, feedback }) {
  const recentWords = Array.isArray(wordsFound)
    ? wordsFound.slice(-6).reverse().map((entry) => (typeof entry === 'string' ? entry : entry.word))
    : [];
  const activeWords = feedback?.words ?? [];

  return (
    <aside className="words-spelled-box">
      <div className="words-spelled-head">
        <span>WORDS SPELLED</span>
        <strong>{Array.isArray(wordsFound) ? wordsFound.length : 0}</strong>
      </div>
      <div className="words-spelled-current">
        {activeWords.length ? activeWords.join(' + ') : recentWords[0] ? `Last word: ${recentWords[0]}` : 'Spell your first word'}
      </div>
      <div className="words-spelled-list">
        {recentWords.length ? recentWords.map((word, index) => <span key={`${word}-${index}`}>{word}</span>) : <span>Waiting for a word</span>}
      </div>
    </aside>
  );
}

function getComboColor(combo) {
  if (combo >= 7) return 'var(--color-gold)';
  if (combo >= 5) return 'var(--color-magenta)';
  if (combo >= 3) return 'var(--color-cyan)';
  return 'var(--color-purpleEnd)';
}

function getComboGradient(combo) {
  if (combo >= 7) return 'linear-gradient(90deg, #cc9900, #ffd60a)';
  if (combo >= 5) return 'linear-gradient(90deg, #cc0055, #ff006e)';
  if (combo >= 3) return 'linear-gradient(90deg, #00b4d8, #00d4ff)';
  return 'linear-gradient(90deg, #4a0e8f, #9d4edd)';
}

function getComboGlow(combo) {
  if (combo >= 7) return '0 0 10px rgba(255,214,10,0.7)';
  if (combo >= 5) return '0 0 10px rgba(255,0,110,0.7)';
  if (combo >= 3) return '0 0 10px rgba(0,212,255,0.7)';
  return '0 0 10px rgba(157,78,221,0.7)';
}

function StatusPanel({ state }) {
  const friendlyMessage = getFriendlyStatus(state);
  return (
    <aside className="status-panel rounded-lg border border-[var(--color-border)] bg-[var(--color-panel)] p-3">
      <div className="flex items-center justify-between gap-3">
        <p className="status-message">{friendlyMessage}</p>
        <span className="font-mono text-sm text-[var(--color-cyan)]">{state.mode.toUpperCase()}</span>
      </div>
      {state.mode === 'timed' && <p className="font-mono text-[var(--color-gold)]">{state.timeLeft}s</p>}
      {state.mode === 'puzzle' && (
        <p className="font-mono text-sm text-[var(--color-gold)]">
          {state.dropsLeft} drops · Targets: {state.targetWords.join(', ') || 'clear'}
        </p>
      )}
    </aside>
  );
}

function getFriendlyStatus(state) {
  if (state.phase !== 'idle') return state.message;
  if (state.feedback) return 'Nice word. Watch the board settle.';
  const wordsFound = Array.isArray(state.wordsFound) ? state.wordsFound : [];
  if (wordsFound.length === 0) return 'Drop letters. Build your first word.';
  if (state.combo >= 3) return 'Keep the streak alive.';
  if (state.nextTiles?.[0]) return `Next letter: ${state.nextTiles[0].letter}. Look for a spot.`;
  return state.message;
}

function VictoryScreen({ state }) {
  const playerWon = state.score >= state.aiScore;
  const floodColor = playerWon ? state.vsSetup.playerColor : state.vsSetup.aiColor;
  const title = playerWon ? (state.vsSetup.teamName || 'PHANTOM FC') : 'CPU OPPONENT';
  const center = state.lastDrop ?? { row: Math.floor(state.board.length / 2), col: Math.floor(COLS / 2) };
  return (
    <section className="victory-screen flex flex-1 flex-col justify-center gap-5" style={{ '--victory-color': floodColor }}>
      <div
        className="victory-board grid grid-cols-7 gap-1.5"
        style={{ aspectRatio: `7 / ${state.board.length}`, gridTemplateRows: `repeat(${state.board.length}, minmax(0, 1fr))` }}
      >
        {state.board.map((row, rowIndex) =>
          row.map((tile, colIndex) => {
            const distance = Math.abs(rowIndex - center.row) + Math.abs(colIndex - center.col);
            return (
              <div
                key={`${rowIndex}-${colIndex}`}
                className="victory-cell"
                style={{ animationDelay: `${distance * 50}ms` }}
              >
                {tile?.letter}
              </div>
            );
          }),
        )}
      </div>
      <div className="victory-burst" style={{ color: floodColor }}>{title}</div>
      <div className="victory-copy">{playerWon ? "THAT'S A CLEAN W NO CAP" : 'Victory secured'}</div>
      <div className="victory-final-score" style={{ color: floodColor }}>{state.score} — {state.aiScore}</div>
    </section>
  );
}

function ResultsScreen({ state, dispatch }) {
  const dailyHistory = state.mode === 'daily' ? JSON.parse(localStorage.getItem('dailyHistory') || '[]') : [];
  const winner = state.vs ? (state.score >= state.aiScore ? 'YOU WIN' : 'AI WINS') : null;
  const prestigeFound = getPrestigeWords(state.wordsFound);
  if (state.vs) {
    const playerBest = bestWord(state.wordsFound);
    const aiBest = bestWord(state.aiWordsFound);
    const rival = getRivalProfile(state.aiDifficulty);
    const playerWon = state.score >= state.aiScore;
    return (
      <section className="flex flex-1 flex-col justify-center gap-5">
        <div className="vs-results-card rounded-lg border border-[var(--color-border)] bg-[var(--color-panel)] p-5 text-center shadow-[0_18px_54px_var(--color-shadow)]">
          <h1 className="font-display text-4xl text-[var(--color-gold)]">{winner}</h1>
          <RankBadge rank={getRank()} className="mt-3" />
          <p className="mt-2 font-mono text-xs uppercase tracking-[3px] text-[var(--color-watermark)]">{playerWon ? rival.loss : rival.win}</p>
          <div className="mt-5 grid grid-cols-[1fr_auto_1fr] items-center gap-3">
            <div style={{ color: state.vsSetup.playerColor }}>
              <div className="font-display text-lg">{state.vsSetup.playerName || 'PLAYER'}</div>
              <div className="font-display text-3xl">{state.score}</div>
              <RoundDots wins={state.playerRounds} color={state.vsSetup.playerColor} />
            </div>
            <div className="font-display text-2xl text-[var(--color-scorePurple)]">VS</div>
            <div className="text-right" style={{ color: state.vsSetup.aiColor }}>
              <div className="font-display text-lg">{rival.name}</div>
              <div className="font-display text-3xl">{state.aiScore}</div>
              <RoundDots wins={state.aiRounds} color={state.vsSetup.aiColor} align="end" />
            </div>
          </div>
          <div className="mt-4 rounded-md bg-[var(--color-background)] p-3 font-mono text-xs text-[var(--color-white)]/75">
            {(state.roundHistory?.length ? state.roundHistory : [{ player: state.score, ai: state.aiScore }]).map((round, index) => (
              <div key={index} className="flex justify-between">
                <span>ROUND {index + 1}</span>
                <span>{round.player} — {round.ai}</span>
              </div>
            ))}
          </div>
          <div className="mt-4 grid grid-cols-2 gap-2 font-mono text-xs text-[var(--color-white)]/75">
            <div>Best You: {playerBest.word} ({playerBest.points})</div>
            <div>Best AI: {aiBest.word} ({aiBest.points})</div>
            <div>Peak Combo: x{state.bestCombo}</div>
            <div>AI Peak: x1</div>
          </div>
          {prestigeFound.length > 0 && <PrestigeSummary words={prestigeFound} />}
        </div>
        <button
          className="rounded-lg bg-[linear-gradient(135deg,var(--color-purpleStart),var(--color-purpleEnd))] py-4 font-display text-lg text-[var(--color-white)] shadow-[0_0_22px_var(--color-purpleGlow)]"
          onClick={() => dispatch({ type: 'REMATCH_VS' })}
        >
          RUN IT BACK
        </button>
        <button
          className="rounded-lg border border-[var(--color-border)] bg-[var(--color-panel)] py-3 font-display text-sm text-[var(--color-cyan)]"
          onClick={() => dispatch({ type: 'SHOW_HOME' })}
        >
          SWITCH IT UP
        </button>
        <StudioTag className="pt-2" />
      </section>
    );
  }
  return (
    <section className="flex flex-1 flex-col justify-center gap-5">
      <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-panel)] p-5 text-center shadow-[0_18px_54px_var(--color-shadow)]">
        <h1 className="font-display text-4xl text-[var(--color-gold)]">{winner ?? 'RESULTS'}</h1>
        <p className="mt-2 font-ui text-xl text-[var(--color-white)]/75">{state.message}</p>
        <div className={`mt-5 grid ${state.vs ? 'grid-cols-2' : 'grid-cols-3'} gap-2`}>
          <ResultStat label="Score" value={state.score} />
          {state.vs && <ResultStat label="AI Score" value={state.aiScore} />}
          {!state.vs && (
            <>
          <ResultStat label="Words" value={state.wordsFound.length} />
          <ResultStat label="Best" value={`x${state.bestCombo}`} />
            </>
          )}
        </div>
        {state.vs && (
          <div className="mt-4 grid grid-cols-2 gap-2 font-mono text-xs text-[var(--color-white)]/75">
            <div>Best You: {bestWord(state.wordsFound).word}</div>
            <div>Best AI: {bestWord(state.aiWordsFound).word}</div>
          </div>
        )}
        <div className="mt-5 max-h-28 overflow-auto rounded-md bg-[var(--color-background)] p-3 font-mono text-sm text-[var(--color-cyan)]">
          {formatWords(state.wordsFound) || 'No words found'}
        </div>
        {prestigeFound.length > 0 && <PrestigeSummary words={prestigeFound} />}
        {state.mode === 'daily' && (
          <div className="mt-4 rounded-md bg-[var(--color-background)] p-3 text-left">
            <div className="font-display text-sm text-[var(--color-gold)]">DAILY ARENA</div>
            <div className="mb-2 font-mono text-xs text-[var(--color-cyan)]">Seed {state.dailyDate} · {localStorage.getItem('dailyStreak') || 1} day streak · same board, one shot</div>
            {dailyHistory.map((entry) => (
              <div key={entry.date} className="flex justify-between font-mono text-xs text-[var(--color-white)]/75">
                <span>{entry.date}</span>
                <span>{entry.score} · {entry.bestWord ?? 'NONE'}</span>
              </div>
            ))}
          </div>
        )}
      </div>
      <ShareTools state={state} />
      <button
        className="rounded-lg bg-[linear-gradient(135deg,var(--color-purpleStart),var(--color-purpleEnd))] py-4 font-display text-lg text-[var(--color-white)] shadow-[0_0_22px_var(--color-purpleGlow)]"
        onClick={() => dispatch({ type: 'START_MODE', mode: state.mode })}
      >
        REPLAY
      </button>
      <button
        className="rounded-lg border border-[var(--color-border)] bg-[var(--color-panel)] py-3 font-display text-sm text-[var(--color-cyan)]"
        onClick={() => dispatch({ type: 'SHOW_HOME' })}
      >
        MODE SELECT
      </button>
      <StudioTag className="pt-2" />
    </section>
  );
}

function ResultStat({ label, value }) {
  return (
    <div className="rounded-md border border-[var(--color-border)] bg-[var(--color-background)] p-3">
      <div className="font-mono text-xs text-[var(--color-purpleText)]">{label}</div>
      <div className="font-display text-lg text-[var(--color-white)]">{value}</div>
    </div>
  );
}

function PrestigeSummary({ words }) {
  return (
    <div className="prestige-summary">
      <div>PRESTIGE WORDS 💎</div>
      <p>{words.join(' · ')}</p>
    </div>
  );
}

function ShareTools({ state }) {
  const [url, setUrl] = useState('');
  const create = async () => {
    const dataUrl = await makeShareCard(state);
    setUrl(dataUrl);
    return dataUrl;
  };
  const download = async () => {
    const dataUrl = url || (await create());
    const link = document.createElement('a');
    link.href = dataUrl;
    link.download = 'neon-connect-words-score.png';
    link.click();
  };
  const copy = async () => {
    const dataUrl = url || (await create());
    const blob = await (await fetch(dataUrl)).blob();
    if (navigator.clipboard?.write && window.ClipboardItem) {
      await navigator.clipboard.write([new ClipboardItem({ [blob.type]: blob })]);
    }
  };
  return (
    <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-panel)] p-3">
      <button className="w-full rounded-md bg-[var(--color-purpleEnd)] py-3 font-display text-sm" onClick={create}>
        SHARE THE W
      </button>
      <div className="mt-2 grid grid-cols-2 gap-2">
        <button className="rounded-md border border-[var(--color-border)] py-2 font-mono text-xs text-[var(--color-gold)]" onClick={download}>
          Download Image
        </button>
        <button className="rounded-md border border-[var(--color-border)] py-2 font-mono text-xs text-[var(--color-cyan)]" onClick={copy}>
          Copy Image
        </button>
      </div>
    </div>
  );
}

async function makeShareCard(state) {
  const canvas = document.createElement('canvas');
  canvas.width = 1080;
  canvas.height = 1350;
  const ctx = canvas.getContext('2d');
  ctx.fillStyle = getCssColor('background');
  ctx.fillRect(0, 0, canvas.width, canvas.height);
  const glow = ctx.createRadialGradient(540, 650, 40, 540, 650, 520);
  glow.addColorStop(0, getCssColor('purpleGlow'));
  glow.addColorStop(1, 'transparent');
  ctx.fillStyle = glow;
  ctx.fillRect(0, 0, canvas.width, canvas.height);
  ctx.textAlign = 'center';
  ctx.font = '900 86px Orbitron';
  ctx.fillStyle = getCssColor('purpleEnd');
  ctx.fillText('NEON', 540, 190);
  ctx.fillStyle = getCssColor('cyan');
  ctx.fillText('CONNECT', 540, 285);
  ctx.fillStyle = getCssColor('white');
  ctx.fillText('WORDS', 540, 380);
  ctx.font = '900 148px Orbitron';
  ctx.fillStyle = getCssColor('gold');
  ctx.fillText(String(state.score), 540, 650);
  const best = bestWord(state.wordsFound);
  ctx.font = '900 42px Orbitron';
  ctx.fillStyle = getCssColor('white');
  ctx.fillText(`BEST WORD ${best.word} · ${best.points}`, 540, 770);
  ctx.fillText(`PEAK COMBO x${state.bestCombo}`, 540, 850);
  const prestigeWords = getPrestigeWords(state.wordsFound);
  if (prestigeWords.length) {
    ctx.fillStyle = getCssColor('gold');
    ctx.fillText(`PRESTIGE ${prestigeWords.slice(0, 3).join(' · ')}`, 540, 920);
  }
  ctx.font = '32px "Share Tech Mono"';
  ctx.fillStyle = getCssColor('cyan');
  ctx.fillText(`${state.mode.toUpperCase()} · ${new Date().toISOString().slice(0, 10)}`, 540, prestigeWords.length ? 1000 : 940);
  if (state.mode === 'daily') {
    ctx.fillStyle = getCssColor('gold');
    ctx.fillText(`DAILY STREAK ${localStorage.getItem('dailyStreak') || 1}`, 540, prestigeWords.length ? 1060 : 1000);
  }
  if (state.vs) {
    const rival = getRivalProfile(state.aiDifficulty);
    ctx.fillStyle = getCssColor('gold');
    ctx.fillText(`VS ${rival.name}`, 540, prestigeWords.length ? 1060 : 1000);
  }
  ctx.font = '28px "Share Tech Mono"';
  ctx.fillStyle = getCssColor('watermark');
  ctx.fillText('Tobar Mix Creations', 540, 1250);
  return canvas.toDataURL('image/png');
}

function getCssColor(name) {
  return getComputedStyle(document.querySelector('main') ?? document.documentElement).getPropertyValue(`--color-${name}`).trim();
}

function formatWords(words) {
  return words.map((entry) => (typeof entry === 'string' ? entry : `${entry.word}(${entry.points})`)).join(' · ');
}

function bestWord(words) {
  return words.reduce(
    (best, entry) => {
      const word = typeof entry === 'string' ? entry : entry.word;
      const points = typeof entry === 'string' ? entry.length : entry.points;
      return points > best.points ? { word, points } : best;
    },
    { word: 'NONE', points: 0 },
  );
}

function getStoredBestScore() {
  const stats = getPlayerStats();
  const scores = [
    Number(stats.bestScore || 0),
    Number(localStorage.getItem('dailyBestScore') || 0),
    Number(localStorage.getItem('dailyScore') || 0),
  ];
  return Math.max(0, ...scores.filter(Number.isFinite));
}

function getPrestigeWords(words) {
  return [...new Set(words
    .map((entry) => (typeof entry === 'string' ? entry : entry.word))
    .filter((word) => PRESTIGE_WORDS.has(String(word).toUpperCase()))
    .map((word) => String(word).toUpperCase()))];
}

function pickContrastColor(playerColor) {
  const pick = colorChoices.find((choice) => choice.color !== playerColor && choice.id === 'inferno') ?? colorChoices.find((choice) => choice.color !== playerColor);
  return pick.color;
}

function getRank() {
  const stored = localStorage.getItem('playerRank');
  const rank = normalizeRank(stored);
  if (stored !== rank.name) localStorage.setItem('playerRank', rank.name);
  return rank;
}

const rankTiers = [
  { id: 'rookie', name: 'ROOKIE', color: '#888780', copy: 'just getting started' },
  { id: 'hustler', name: 'HUSTLER', color: '#c77dff', copy: 'lowkey improving fr' },
  { id: 'sharp', name: 'SHARP', color: '#00d4ff', copy: 'diff level bussin' },
  { id: 'elite', name: 'ELITE', color: '#ff006e', copy: "it's giving champion" },
  { id: 'neon-god', name: 'NEON GOD', color: '#ffd60a', copy: 'no cap the GOAT era' },
];

function normalizeRank(value) {
  const stored = String(value || '').trim();
  return rankTiers.find((rank) => rank.name === stored || rank.id === stored.toLowerCase()) ?? rankTiers[0];
}

function RankBadge({ rank, className = '' }) {
  return (
    <div className={`rank-badge ${className}`} style={{ '--rank-color': rank.color }}>
      {rank.name}
    </div>
  );
}

function getPlayerStats() {
  const stats = JSON.parse(localStorage.getItem('playerStats') || '{"matches":0,"wins":0,"losses":0,"bestScore":0,"bestWord":"","longestStreak":0,"lossStreak":0,"prestigeWords":[]}');
  return { ...stats, prestigeWords: stats.prestigeWords ?? [] };
}

function updatePlayerStats(state) {
  const stats = getPlayerStats();
  const playerWon = state.score >= state.aiScore;
  const best = bestWord(state.wordsFound);
  const prestigeWords = [...new Set([...(stats.prestigeWords ?? []), ...getPrestigeWords(state.wordsFound)])];
  const next = {
    ...stats,
    matches: stats.matches + 1,
    wins: stats.wins + (playerWon ? 1 : 0),
    losses: stats.losses + (playerWon ? 0 : 1),
    lossStreak: playerWon ? 0 : (stats.lossStreak ?? 0) + 1,
    bestScore: Math.max(stats.bestScore ?? 0, state.score ?? 0),
    bestWord: best.points > (stats.bestWordPoints ?? 0) ? best.word : stats.bestWord,
    bestWordPoints: Math.max(best.points, stats.bestWordPoints ?? 0),
    longestStreak: Math.max(stats.longestStreak ?? 0, state.streak ?? 0),
    prestigeWords,
  };
  localStorage.setItem('playerStats', JSON.stringify(next));
  const currentRank = normalizeRank(localStorage.getItem('playerRank'));
  const currentIndex = Math.max(0, rankTiers.findIndex((rank) => rank.name === currentRank.name));
  const nextIndex = playerWon ? Math.min(rankTiers.length - 1, currentIndex + 1) : next.lossStreak >= 3 ? Math.max(0, currentIndex - 1) : currentIndex;
  const nextRank = rankTiers[nextIndex];
  localStorage.setItem('playerRank', nextRank.name);
  if (nextIndex > currentIndex) return { type: 'up', from: currentRank, to: nextRank };
  if (nextIndex < currentIndex) return { type: 'down', from: currentRank, to: nextRank };
  return null;
}

function StudioTag({ className = '' }) {
  return <div className={`text-center font-mono text-[11px] uppercase tracking-[3px] text-[var(--color-watermark)] ${className}`}>Tobar Mix Creations</div>;
}

function wait(ms) {
  return new Promise((resolve) => window.setTimeout(resolve, ms));
}

function getOffsetDate(offsetDays) {
  const date = new Date();
  date.setDate(date.getDate() + offsetDays);
  return date.toISOString().slice(0, 10);
}

function getDroppedTile(state) {
  if (!state.lastDrop) return null;
  return state.board[state.lastDrop.row]?.[state.lastDrop.col] ?? null;
}

function getTileSoundColor(tile) {
  if (!tile) return 'purple';
  if (tile.variant === 'ai' || tile.owner === 'ai') return 'red';
  if (tile.status === 'clearing') return 'gold';
  return tile.variant ?? 'purple';
}

function findDroppedBomb(board) {
  for (let row = 0; row < board.length; row += 1) {
    for (let col = 0; col < COLS; col += 1) {
      const tile = board[row][col];
      if (tile?.special === 'bomb' && tile.justDropped) return { row, col };
    }
  }
  return null;
}

function getDropCinematicCue(state) {
  if (state.screen !== 'game' || state.phase !== 'dropping' || !state.lastDrop || !state.cinematicDrop) return null;
  return {
    ...state.cinematicDrop,
    cells: new Set(state.cinematicDrop.cells.map((cell) => `${cell.row},${cell.col}`)),
  };
}

function getCinematicDuration(length) {
  if (length <= 2) return 600;
  if (length === 3) return 800;
  if (length === 4) return 1000;
  if (length === 5) return 1200;
  return 1400;
}

function getImpactTier(length) {
  if (length >= 7) return 'prestige';
  if (length >= 6) return 'massive';
  if (length >= 5) return 'heavy';
  if (length >= 4) return 'strong';
  if (length >= 3) return 'medium';
  return 'small';
}

function chooseAiColumn(state) {
  const available = Array.from({ length: COLS }, (_, col) => col).filter((col) => !state.board[0][col]);
  if (!available.length) return 0;
  if (state.aiDifficulty === 'easy') return available[Math.floor(Math.random() * available.length)];
  if (state.aiDifficulty === 'medium' && Math.random() < 0.3) return available[Math.floor(Math.random() * available.length)];
  const playerBlock = state.aiDifficulty === 'hard' ? findPlayerBlockColumn(state, available) : null;
  if (playerBlock !== null) return playerBlock;
  let best = available[0];
  let bestScore = -1;
  available.forEach((col) => {
    const row = findDropRowLocal(state.board, col);
    const board = state.board.map((line) => line.slice());
    board[row][col] = makeTile({ owner: 'ai', variant: 'ai' });
    const aiState = { ...state, board, lastActor: 'ai', lastDrop: { row, col } };
    const immediateScore = getScoringMatchesForState(aiState, findMatches(board, { minLength: 2, vs: true })).reduce((sum, match) => sum + match.word.length, 0);
    const setupScore = countSequencesThroughDrop(board, { row, col }, 'ai');
    const score = state.aiDifficulty === 'medium' ? setupScore : immediateScore * 10 + setupScore;
    if (score > bestScore) {
      bestScore = score;
      best = col;
    }
  });
  return best;
}

function findPlayerBlockColumn(state, available) {
  for (const col of available) {
    const row = findDropRowLocal(state.board, col);
    const next = state.board.map((line) => line.slice());
    next[row][col] = makeTile({ owner: 'player', variant: 'purple' });
    const playerState = { ...state, board: next, lastActor: 'player', lastDrop: { row, col } };
    const wouldScore = getScoringMatchesForState(playerState, findMatches(next, { minLength: 2, vs: true })).length > 0;
    if (wouldScore) return col;
  }
  return null;
}

function countSequencesThroughDrop(board, drop, owner) {
  const directions = [
    [0, 1],
    [1, 0],
    [1, 1],
    [1, -1],
  ];
  return directions.reduce((count, [dr, dc]) => {
    let length = 1;
    for (const dir of [-1, 1]) {
      let row = drop.row + dr * dir;
      let col = drop.col + dc * dir;
      while (row >= 0 && row < board.length && col >= 0 && col < COLS && board[row][col]?.owner === owner) {
        length += 1;
        row += dr * dir;
        col += dc * dir;
      }
    }
    return count + (length >= 2 ? length : 0);
  }, 0);
}

function getConnectChainTrails(state) {
  if (!state.vs || state.screen !== 'game') return [];
  const rows = state.board.length;
  const wordCellSets = findMatches(state.board, { minLength: 2, vs: true }).map((match) => new Set(match.cells.map((cell) => `${cell.row},${cell.col}`)));
  const directions = [
    [0, 1],
    [1, 0],
    [1, 1],
    [1, -1],
  ];
  const trails = [];
  const isWordCells = (cells) => wordCellSets.some((set) => cells.every((cell) => set.has(`${cell.row},${cell.col}`)));

  for (let row = 0; row < rows; row += 1) {
    for (let col = 0; col < COLS; col += 1) {
      const tile = state.board[row][col];
      if (!tile?.owner) continue;
      for (const [dr, dc] of directions) {
        const prevRow = row - dr;
        const prevCol = col - dc;
        if (state.board[prevRow]?.[prevCol]?.owner === tile.owner) continue;
        const cells = [];
        let nextRow = row;
        let nextCol = col;
        while (nextRow >= 0 && nextRow < rows && nextCol >= 0 && nextCol < COLS && state.board[nextRow][nextCol]?.owner === tile.owner) {
          cells.push({ row: nextRow, col: nextCol });
          nextRow += dr;
          nextCol += dc;
        }
        if (cells.length < 2 || isWordCells(cells)) continue;
        for (let index = 0; index < cells.length - 1; index += 1) {
          const from = cells[index];
          const to = cells[index + 1];
          trails.push({
            owner: tile.owner,
            color: tile.owner === 'ai' ? 'rgba(0,255,65,0.3)' : colorToRgba(state.vsSetup.playerColor, 0.3),
            x1: from.col + 0.5,
            y1: from.row + 0.5,
            x2: to.col + 0.5,
            y2: to.row + 0.5,
          });
        }
      }
    }
  }
  return trails.slice(0, 48);
}

function colorToRgba(hex, alpha) {
  const clean = String(hex || '#9d4edd').replace('#', '');
  const value = clean.length === 3 ? clean.split('').map((char) => char + char).join('') : clean;
  const red = Number.parseInt(value.slice(0, 2), 16);
  const green = Number.parseInt(value.slice(2, 4), 16);
  const blue = Number.parseInt(value.slice(4, 6), 16);
  if ([red, green, blue].some((part) => Number.isNaN(part))) return `rgba(157,78,221,${alpha})`;
  return `rgba(${red},${green},${blue},${alpha})`;
}

function findDropRowLocal(board, col) {
  for (let row = board.length - 1; row >= 0; row -= 1) {
    if (!board[row][col]) return row;
  }
  return -1;
}

function createAudioEngine() {
  let context;
  let muted = false;
  const getContext = () => {
    if (!context) {
      const AudioCtor = window.AudioContext || window.webkitAudioContext;
      context = new AudioCtor();
    }
    return context;
  };
  const unlock = () => {
    const ctx = getContext();
    if (ctx.state === 'suspended') ctx.resume();
  };
  const canPlay = () => !muted && typeof window !== 'undefined' && (window.AudioContext || window.webkitAudioContext);
  const now = () => getContext().currentTime;
  const gainNode = (gain = 0.12, destination = getContext().destination) => {
    const ctx = getContext();
    const node = ctx.createGain();
    node.gain.value = gain;
    node.connect(destination);
    return node;
  };
  const exponentialToZero = (param, t, seconds) => param.exponentialRampToValueAtTime(0.001, t + seconds);

  return {
    setMuted(value) {
      muted = Boolean(value);
    },
    unlock,
    drop(color = 'purple') {
      if (!canPlay()) return;
      const ctx = getContext();
      const t = now();
      const pitchMap = { purple: 118, cyan: 152, magenta: 132, gold: 176, red: 96, green: 144 };
      const basePitch = pitchMap[color] || 118;

      const thud = ctx.createOscillator();
      const thudGain = gainNode(0);
      const thudFilter = ctx.createBiquadFilter();
      thudFilter.type = 'lowpass';
      thudFilter.frequency.setValueAtTime(260, t);
      thudFilter.frequency.exponentialRampToValueAtTime(95, t + 0.18);
      thud.type = 'sine';
      thud.frequency.setValueAtTime(basePitch, t);
      thud.frequency.exponentialRampToValueAtTime(54, t + 0.18);
      thudGain.gain.setValueAtTime(0.0001, t);
      thudGain.gain.exponentialRampToValueAtTime(0.62, t + 0.012);
      exponentialToZero(thudGain.gain, t, 0.24);
      thud.connect(thudFilter);
      thudFilter.connect(thudGain);
      thud.start(t);
      thud.stop(t + 0.26);

      const tick = ctx.createOscillator();
      const tickGain = gainNode(0);
      const tickFilter = ctx.createBiquadFilter();
      tickFilter.type = 'bandpass';
      tickFilter.frequency.value = 1800;
      tickFilter.Q.value = 7;
      tick.type = 'triangle';
      tick.frequency.setValueAtTime(basePitch * 6, t + 0.018);
      tick.frequency.exponentialRampToValueAtTime(basePitch * 9, t + 0.13);
      tickGain.gain.setValueAtTime(0.0001, t + 0.018);
      tickGain.gain.exponentialRampToValueAtTime(0.16, t + 0.035);
      exponentialToZero(tickGain.gain, t + 0.035, 0.18);
      tick.connect(tickFilter);
      tickFilter.connect(tickGain);
      tick.start(t + 0.018);
      tick.stop(t + 0.24);

      const sparkleDelay = ctx.createDelay();
      sparkleDelay.delayTime.value = 0.055;
      const sparkleFeedback = gainNode(0.18, sparkleDelay);
      const sparkleOut = gainNode(0.14);
      sparkleDelay.connect(sparkleOut);
      tickGain.connect(sparkleFeedback);
    },
    word(length) {
      if (!canPlay()) return;
      const ctx = getContext();
      const notes = [261, 329, 392];
      const pitchMult = 1 + length * 0.08;
      notes.forEach((freq, index) => {
        const t = now() + index * 0.04;
        const osc = ctx.createOscillator();
        const gain = gainNode(0);
        osc.type = 'triangle';
        osc.frequency.value = freq * pitchMult;
        gain.gain.setValueAtTime(0, t);
        gain.gain.linearRampToValueAtTime(0.3, t + 0.02);
        exponentialToZero(gain.gain, t, 0.6);
        osc.connect(gain);
        osc.start(t);
        osc.stop(t + 0.7);
      });
      const t = now() + 0.1;
      const shimmer = ctx.createOscillator();
      const shimmerGain = gainNode(0);
      shimmer.type = 'sine';
      shimmer.frequency.value = 880 * pitchMult;
      shimmerGain.gain.setValueAtTime(0.15, t);
      exponentialToZero(shimmerGain.gain, t, 0.5);
      shimmer.connect(shimmerGain);
      shimmer.start(t);
      shimmer.stop(t + 0.55);
    },
    crush() {
      if (!canPlay()) return;
      const ctx = getContext();
      const t = now();
      const bufferSize = Math.floor(ctx.sampleRate * 0.15);
      const buffer = ctx.createBuffer(1, bufferSize, ctx.sampleRate);
      const data = buffer.getChannelData(0);
      for (let i = 0; i < bufferSize; i += 1) data[i] = Math.random() * 2 - 1;
      const noise = ctx.createBufferSource();
      const filter = ctx.createBiquadFilter();
      const gain = gainNode(0.8);
      filter.type = 'bandpass';
      filter.frequency.value = 800;
      noise.buffer = buffer;
      exponentialToZero(gain.gain, t, 0.15);
      noise.connect(filter);
      filter.connect(gain);
      noise.start(t);
      noise.stop(t + 0.15);

      const punch = ctx.createOscillator();
      const punchGain = gainNode(0.8);
      punch.type = 'sine';
      punch.frequency.setValueAtTime(180, t);
      punch.frequency.exponentialRampToValueAtTime(40, t + 0.12);
      exponentialToZero(punchGain.gain, t, 0.12);
      punch.connect(punchGain);
      punch.start(t);
      punch.stop(t + 0.14);
    },
    combo(combo) {
      if (!canPlay()) return;
      const ctx = getContext();
      const t = now();
      const osc = ctx.createOscillator();
      const delay = ctx.createDelay();
      const delayGain = ctx.createGain();
      const filter = ctx.createBiquadFilter();
      const gain = gainNode(0.5);
      filter.type = 'lowpass';
      filter.frequency.value = 2000 + combo * 200;
      osc.type = 'sawtooth';
      osc.frequency.setValueAtTime(220 + combo * 55, t);
      osc.frequency.exponentialRampToValueAtTime((220 + combo * 55) * 1.5, t + 0.08);
      delay.delayTime.value = 0.12;
      delayGain.gain.value = 0.25;
      osc.connect(filter);
      filter.connect(gain);
      gain.connect(delay);
      delay.connect(delayGain);
      delayGain.connect(ctx.destination);
      exponentialToZero(gain.gain, t, 0.35);
      osc.start(t);
      osc.stop(t + 0.4);
    },
    block() {
      if (!canPlay()) return;
      const ctx = getContext();
      const t = now();
      const filter = ctx.createBiquadFilter();
      const gain = gainNode(0.4);
      filter.type = 'highpass';
      filter.frequency.value = 800;
      [440, 448].forEach((freq) => {
        const osc = ctx.createOscillator();
        osc.type = 'square';
        osc.frequency.value = freq;
        osc.connect(filter);
        osc.start(t);
        osc.stop(t + 0.3);
      });
      exponentialToZero(gain.gain, t, 0.25);
      filter.connect(gain);
      const thud = ctx.createOscillator();
      const thudGain = gainNode(0.7);
      thud.type = 'sine';
      thud.frequency.setValueAtTime(80, t);
      thud.frequency.exponentialRampToValueAtTime(30, t + 0.1);
      exponentialToZero(thudGain.gain, t, 0.1);
      thud.connect(thudGain);
      thud.start(t);
      thud.stop(t + 0.15);
    },
    fever() {
      if (!canPlay()) return;
      const ctx = getContext();
      const t = now();
      const sweep = ctx.createOscillator();
      const filter = ctx.createBiquadFilter();
      const gain = gainNode(0.5);
      filter.type = 'lowpass';
      filter.frequency.setValueAtTime(500, t);
      filter.frequency.exponentialRampToValueAtTime(4000, t + 0.6);
      sweep.type = 'sawtooth';
      sweep.frequency.setValueAtTime(80, t);
      sweep.frequency.exponentialRampToValueAtTime(640, t + 0.6);
      gain.gain.linearRampToValueAtTime(0.7, t + 0.3);
      exponentialToZero(gain.gain, t, 0.8);
      sweep.connect(filter);
      filter.connect(gain);
      sweep.start(t);
      sweep.stop(t + 0.8);
      [261, 329, 392, 523].forEach((freq) => {
        const osc = ctx.createOscillator();
        const noteGain = gainNode(0);
        osc.type = 'triangle';
        osc.frequency.value = freq;
        noteGain.gain.setValueAtTime(0, t + 0.5);
        noteGain.gain.linearRampToValueAtTime(0.25, t + 0.55);
        exponentialToZero(noteGain.gain, t + 0.5, 0.7);
        osc.connect(noteGain);
        osc.start(t + 0.5);
        osc.stop(t + 1.25);
      });
    },
    victory() {
      if (!canPlay()) return;
      const ctx = getContext();
      const t = now();
      [261, 329, 392, 523, 659].forEach((freq, index) => {
        const start = t + index * 0.15;
        const osc = ctx.createOscillator();
        const gain = gainNode(0.4);
        osc.type = 'triangle';
        osc.frequency.value = freq;
        exponentialToZero(gain.gain, start, 0.4);
        osc.connect(gain);
        osc.start(start);
        osc.stop(start + 0.5);
      });
    },
    bomb() {
      if (!canPlay()) return;
      const ctx = getContext();
      const buffer = ctx.createBuffer(1, ctx.sampleRate * 0.35, ctx.sampleRate);
      const data = buffer.getChannelData(0);
      for (let i = 0; i < data.length; i += 1) data[i] = Math.random() * 2 - 1;
      const noise = ctx.createBufferSource();
      const filter = ctx.createBiquadFilter();
      const gain = gainNode(0.2);
      const t = now();
      filter.type = 'lowpass';
      filter.frequency.setValueAtTime(900, t);
      filter.frequency.exponentialRampToValueAtTime(80, t + 0.35);
      gain.gain.exponentialRampToValueAtTime(0.001, t + 0.34);
      noise.buffer = buffer;
      noise.connect(filter);
      filter.connect(gain);
      noise.start(t);
      noise.stop(t + 0.35);
    },
  };
}
