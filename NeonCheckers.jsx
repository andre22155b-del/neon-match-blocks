/**
 * ============================================================
 * NEON CHECKERS — React Native (Expo)  v2.0
 * ============================================================
 *
 * FILE: NeonCheckers.jsx
 * ENTRY: Default export → <NeonCheckers />
 *
 * ── SETUP ────────────────────────────────────────────────────
 *   npx create-expo-app NeonCheckers --template blank
 *   cd NeonCheckers
 *   npx expo install expo-linear-gradient
 *   # Rename App.js → App.jsx, replace contents with this file
 *   npx expo start
 *
 * ── DEPENDENCIES ─────────────────────────────────────────────
 *   expo-linear-gradient  → board gradients & UI panels
 *   react-native core     → Animated, TouchableOpacity, Modal
 *   Web Audio API         → procedural SFX on web (native falls back silently)
 *
 * ── ARCHITECTURE ─────────────────────────────────────────────
 *   NeonCheckers          Root component — manages all state
 *   ├── MenuScreen        Mode selection (local / vs AI / rules)
 *   ├── RulesScreen       In-app rules reference
 *   ├── GameScreen        Main gameplay view
 *   │   ├── ScoreBar      Player stats + active turn indicator
 *   │   ├── VRBoard       3-D perspective board (CSS transform)
 *   │   │   └── Cell      Individual square + piece + move hints
 *   │   ├── TurnBanner    Current player / must-jump alert
 *   │   └── MoveLog       Last 5 move history entries
 *   ├── WinModal          End-game overlay with stats
 *   ├── GlitchText        Chromatic-aberration title effect
 *   └── Scanlines         CRT scanline overlay
 *
 * ── GAME RULES (TRADITIONAL AMERICAN CHECKERS) ───────────────
 *   See RULES object and RulesScreen below.
 *
 * ── AI ENGINE ────────────────────────────────────────────────
 *   Minimax with Alpha-Beta pruning, default depth 4.
 *   Evaluation: piece count + positional + king weight + edge safety.
 *   Tune difficulty via the AI_DEPTH constant below.
 *
 * ── VR PERSPECTIVE ───────────────────────────────────────────
 *   Board is rendered with CSS perspective + rotateX to simulate
 *   a 3-D camera angle (~32° tilt). In local 2-player mode the
 *   board animates a 180° flip between turns so each player
 *   always faces their own pieces. Pieces compensate for the
 *   perspective skew with a scaleY correction factor.
 *
 * ── CODEX EXTENSION POINTS ───────────────────────────────────
 *   Search "CODEX:" in this file for suggested tasks:
 *   - CODEX: multiplayer via WebSocket / Supabase Realtime
 *   - CODEX: persist stats / leaderboard with AsyncStorage
 *   - CODEX: Three.js / r3f full 3-D board upgrade
 *   - CODEX: expo-haptics for tactile feedback on moves
 *   - CODEX: difficulty slider (change AI_DEPTH 1-6)
 *   - CODEX: per-turn countdown timer
 *   - CODEX: replay system (record + replay move history)
 *   - CODEX: implement Neon Variant game modes (see RULES object)
 *   - CODEX: gyroscope board tilt (expo-sensors DeviceMotion)
 *
 * ============================================================
 */

import React, { useState, useEffect, useRef } from "react";
import {
  View,
  Text,
  TouchableOpacity,
  StyleSheet,
  Dimensions,
  Animated,
  Modal,
  StatusBar,
  SafeAreaView,
  ScrollView,
  Platform,
} from "react-native";
import { LinearGradient } from "expo-linear-gradient";

// ─────────────────────────────────────────────────────────────
// CONSTANTS
// ─────────────────────────────────────────────────────────────

const { width: SW, height: SH } = Dimensions.get("window");

/** Board pixel size — responsive, leaves horizontal padding. */
const BOARD_SIZE = Math.min(SW - 20, SH * 0.52);
const CELL = BOARD_SIZE / 8;
const QUIET_TURN_DRAW_LIMIT = 40;

/**
 * AI_DEPTH — Minimax search depth.
 *   2 = easy (beginner)
 *   4 = medium (default, strong amateur)
 *   6 = hard (slow on older devices)
 * CODEX: expose this via a difficulty picker in Settings.
 */
const AI_DEPTH = 4;

/**
 * VR Camera constants.
 *   PERSPECTIVE  — focal length in px (lower = more dramatic)
 *   TILT         — rotateX in degrees (0 = flat, 45 = steep)
 */
const PERSPECTIVE = 900;
const TILT        = 32; // degrees
const WEB_AUDIO_SUPPORTED =
  Platform.OS === "web" &&
  (typeof AudioContext !== "undefined" || typeof webkitAudioContext !== "undefined");

// ─────────────────────────────────────────────────────────────
// COLOR PALETTE — Hot Magenta × Electric Lime × Void Black
// ─────────────────────────────────────────────────────────────

const C = {
  bg:           "#050508",
  boardDark:    "#0B0B1C",   // playable dark squares
  boardLight:   "#13132B",   // decorative light squares
  p1:           "#FF2D78",   // Player 1 — hot magenta
  p1Glow:       "#FF2D7870",
  p1King:       "#FF85B3",
  p2:           "#AAFF00",   // Player 2 / AI — electric lime
  p2Glow:       "#AAFF0070",
  p2King:       "#CCFF66",
  cyan:         "#00EEFF",   // move highlight / UI accent
  cyanFaint:    "#00EEFF20",
  captureFaint: "#FF2D7828",
  text:         "#FFFFFF",
  textDim:      "#44446A",
  uiBorder:     "#1E1E40",
  uiBg:         "#09091A",
  scanline:     "#FFFFFF04",
  violet:       "#7B2FFF",
};

// ─────────────────────────────────────────────────────────────
// RULES — machine-readable reference object
// Used by RulesScreen and exported for Codex / test use.
// ─────────────────────────────────────────────────────────────

export const RULES = {
  version: "American Checkers (Draughts) — Official Standard Ruleset",

  board: {
    description: "8×8 grid. Pieces occupy and move only on dark squares.",
    startingPieces: 12,
    p1Rows: "Rows 6–8 (bottom, closest to player)",
    p2Rows: "Rows 1–3 (top, farthest from player)",
    firstMove: "Player 1 (Magenta) always moves first.",
  },

  movement: {
    standard:  "Regular pieces move diagonally forward one square to an adjacent empty dark square.",
    king:      "Kings may move diagonally in ANY direction — forward or backward.",
    noJump:    "A piece may not move to a square occupied by any other piece (enemy or friendly).",
    noPass:    "A player may never voluntarily pass their turn.",
    promotion: "A piece that reaches the opponent's back row is immediately crowned a King (♛). Promotion ends the turn — the newly crowned King may NOT continue jumping in the same move.",
  },

  capturing: {
    howToJump:    "A piece captures by jumping diagonally over an adjacent enemy piece to a vacant square immediately beyond it.",
    mandatory:    "⚡ MANDATORY JUMP — If any jump is available for your pieces, you MUST make a jump. You cannot make a simple move instead.",
    choiceOfJump: "If multiple pieces can jump, you choose WHICH piece to move. But a jump must be made.",
    multiJump:    "After a successful jump, if the same piece can immediately jump again, it MUST continue jumping (chain jump). The turn only ends when no further jump is possible.",
    kingCapture:  "Kings may capture in all four diagonal directions, including backward jumps.",
    removal:      "Captured pieces remain on the board until the full turn (including chain jumps) is complete, then are removed together.",
    cantLand:     "You cannot jump over or land on a friendly piece.",
  },

  winning: {
    captureAll: "Capture all 12 of your opponent's pieces.",
    blockAll:   "Leave your opponent with no legal move on their turn (all pieces blocked).",
    draw:       "A draw may be agreed by both players in standard play. In this build, a draw is automatically declared after 40 consecutive turns without a capture or promotion.",
  },

  keyRules: [
    "Pieces travel and land only on dark squares — never on light ones.",
    "Player 1 (Magenta) moves first in every game.",
    "Mandatory jump: you cannot ignore an available capture.",
    "Chain jumps: the same piece must keep jumping if it can.",
    "A piece is kinged the moment it reaches the last row — promotion ends the turn.",
    "Captured pieces are removed after the full turn concludes.",
    "You may never pass your turn voluntarily.",
  ],

  neonVariants: [
    "⚡ BLITZ MODE — Each player has 15 seconds per turn. Miss your turn clock = forfeit one piece.",
    "🔥 CHAOS MODE — Every 5 turns a random dark square is 'electrified'. Landing on it skips your next turn.",
    "👑 KING'S GAMBIT — Start with 2 Kings each instead of 12 normal pieces for a tactical knife-fight.",
    "💥 NUKE RULE — Once per game, a player may remove any one enemy piece without jumping (no capture credit).",
    "🌀 MIRROR MATCH — Both players share the same set of 12 pieces; capturing your own piece is legal and removes it.",
  ],
};

// ─────────────────────────────────────────────────────────────
// SOUND ENGINE — Procedural Web Audio (zero asset files)
// CODEX: swap with expo-av + sampled sounds for richer SFX
// ─────────────────────────────────────────────────────────────

let _ctx = null;
function getCtx() {
  if (!WEB_AUDIO_SUPPORTED) return null;
  if (_ctx) return _ctx;
  if (typeof AudioContext !== "undefined")           _ctx = new AudioContext();
  else if (typeof webkitAudioContext !== "undefined") _ctx = new webkitAudioContext(); // eslint-disable-line
  return _ctx;
}

/**
 * playTone — synthesise a brief oscillator note.
 * @param {number} freq - Hz
 * @param {"sine"|"square"|"sawtooth"|"triangle"} type
 * @param {number} dur  - duration seconds
 * @param {number} gain - peak volume 0–1
 * @param {number} delay - seconds before start
 */
function playTone(freq, type = "sine", dur = 0.12, gain = 0.15, delay = 0) {
  try {
    const ctx = getCtx();
    if (!ctx) return;
    const osc = ctx.createOscillator();
    const gn  = ctx.createGain();
    osc.connect(gn);
    gn.connect(ctx.destination);
    osc.type = type;
    osc.frequency.setValueAtTime(freq, ctx.currentTime + delay);
    gn.gain.setValueAtTime(0,    ctx.currentTime + delay);
    gn.gain.linearRampToValueAtTime(gain,  ctx.currentTime + delay + 0.01);
    gn.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + delay + dur);
    osc.start(ctx.currentTime + delay);
    osc.stop(ctx.currentTime  + delay + dur + 0.05);
  } catch (_) { /* audio unavailable — silent fallback */ }
}

const SFX = {
  select:  () => playTone(660, "sine",     0.06, 0.08),
  move:    () => { playTone(440, "square", 0.08, 0.09); playTone(550, "sine", 0.07, 0.05, 0.06); },
  capture: () => { playTone(200, "sawtooth", 0.14, 0.2); playTone(120, "square", 0.18, 0.12, 0.10); },
  king:    () => [523,659,784,1047].forEach((f,i) => playTone(f, "sine",     0.15, 0.15, i*0.09)),
  win:     () => [523,659,784,1047,1319].forEach((f,i) => playTone(f, "triangle", 0.25, 0.2,  i*0.10)),
  invalid: () => { playTone(180,"square",0.10,0.12); playTone(155,"square",0.10,0.10,0.07); },
  flip:    () => playTone(330, "sine", 0.18, 0.05),
};

// ─────────────────────────────────────────────────────────────
// GAME LOGIC — Pure functions, no side effects
// ─────────────────────────────────────────────────────────────

/** Board cell values */
const EMPTY = 0, P1 = 1, P2 = 2, P1K = 3, P2K = 4;

/** Predicates */
const isKing    = (p) => p === P1K || p === P2K;
const isP1Piece = (p) => p === P1  || p === P1K;
const isP2Piece = (p) => p === P2  || p === P2K;
const ownedBy   = (p, player) => player === 1 ? isP1Piece(p) : isP2Piece(p);
const inBounds  = (r, c) => r >= 0 && r < 8 && c >= 0 && c < 8;

/** Create a fresh 8×8 starting board */
function initBoard() {
  const b = Array(8).fill(null).map(() => Array(8).fill(EMPTY));
  for (let r = 0; r < 3; r++)
    for (let c = 0; c < 8; c++)
      if ((r + c) % 2 === 1) b[r][c] = P2;
  for (let r = 5; r < 8; r++)
    for (let c = 0; c < 8; c++)
      if ((r + c) % 2 === 1) b[r][c] = P1;
  return b;
}

/**
 * directions — diagonal move directions for a piece type.
 * P1 pieces move toward row 0 (up). P2 toward row 7 (down).
 * Kings move all 4 diagonals.
 */
function directions(piece) {
  if (piece === P1)  return [[-1,-1],[-1,1]];
  if (piece === P2)  return [[ 1,-1],[ 1,1]];
  return [[-1,-1],[-1,1],[1,-1],[1,1]];
}

/**
 * getJumps — all single-hop capture moves available for piece at (r,c).
 * Returns: Array<{ to:[r,c], captured:[r,c] }>
 */
function getJumps(board, r, c) {
  const piece = board[r][c];
  const owner = isP1Piece(piece) ? 1 : 2;
  return directions(piece).reduce((acc, [dr, dc]) => {
    const [mr, mc] = [r+dr, c+dc];
    const [lr, lc] = [r+2*dr, c+2*dc];
    if (
      inBounds(lr, lc)        &&
      board[lr][lc] === EMPTY &&
      inBounds(mr, mc)        &&
      board[mr][mc] !== EMPTY &&
      !ownedBy(board[mr][mc], owner)
    ) acc.push({ to:[lr,lc], captured:[mr,mc] });
    return acc;
  }, []);
}

/**
 * getMoves — all simple (non-capture) diagonal moves for piece at (r,c).
 * Returns: Array<{ to:[r,c], captured:null }>
 */
function getMoves(board, r, c) {
  return directions(board[r][c])
    .map(([dr,dc]) => [r+dr, c+dc])
    .filter(([nr,nc]) => inBounds(nr,nc) && board[nr][nc] === EMPTY)
    .map((to) => ({ to, captured: null }));
}

/**
 * getAllMoves — full legal move list for a player.
 * Enforces mandatory-jump rule: if any jump exists, ONLY jumps are returned.
 */
function getAllMoves(board, player) {
  const jumps = [], moves = [];
  for (let r = 0; r < 8; r++)
    for (let c = 0; c < 8; c++)
      if (ownedBy(board[r][c], player)) {
        getJumps(board, r, c).forEach((j) => jumps.push({ from:[r,c], ...j }));
        getMoves(board, r, c).forEach((m) => moves.push({ from:[r,c], ...m }));
      }
  return jumps.length > 0 ? jumps : moves;
}

/**
 * applyMove — return a new board with the move applied.
 * Pure function — does NOT mutate the original board.
 * Handles: piece move, capture removal, king promotion.
 */
function applyMove(board, move) {
  const nb = board.map((row) => [...row]);
  const [fr,fc] = move.from;
  const [tr,tc] = move.to;
  nb[tr][tc] = nb[fr][fc];
  nb[fr][fc] = EMPTY;
  if (move.captured) nb[move.captured[0]][move.captured[1]] = EMPTY;
  if (nb[tr][tc] === P1 && tr === 0) nb[tr][tc] = P1K; // P1 reaches row 0 → King
  if (nb[tr][tc] === P2 && tr === 7) nb[tr][tc] = P2K; // P2 reaches row 7 → King
  return nb;
}

/** Detect whether this move crowned a new king. */
function didPromote(boardBefore, boardAfter, move, player) {
  return (
    boardBefore[move.from[0]][move.from[1]] === (player === 1 ? P1 : P2) &&
    isKing(boardAfter[move.to[0]][move.to[1]])
  );
}

/**
 * getJumpSequences — all full capture turns for a piece at (r,c).
 * Multi-jumps are expanded into complete turn sequences for AI search.
 */
function getJumpSequences(board, r, c, origin = [r, c]) {
  const piece = board[r][c];
  const player = isP1Piece(piece) ? 1 : 2;
  const jumps = getJumps(board, r, c);
  if (!jumps.length) return [];

  return jumps.flatMap((jump) => {
    const step = { from:[r, c], ...jump };
    const nextBoard = applyMove(board, step);

    // Promotion ends the turn immediately in American checkers.
    if (didPromote(board, nextBoard, step, player)) {
      return [{
        from: origin,
        to: step.to,
        captured: [step.captured],
        sequence: [step],
      }];
    }

    const continuations = getJumpSequences(nextBoard, step.to[0], step.to[1], origin);
    if (!continuations.length) {
      return [{
        from: origin,
        to: step.to,
        captured: [step.captured],
        sequence: [step],
      }];
    }

    return continuations.map((continuation) => ({
      from: origin,
      to: continuation.to,
      captured: [step.captured, ...continuation.captured],
      sequence: [step, ...continuation.sequence],
    }));
  });
}

/**
 * getAllTurnMoves — full legal turns for a player.
 * Capture turns are returned as complete mandatory chain sequences.
 */
function getAllTurnMoves(board, player) {
  const jumps = [];
  const moves = [];

  for (let r = 0; r < 8; r++)
    for (let c = 0; c < 8; c++)
      if (ownedBy(board[r][c], player)) {
        const jumpSequences = getJumpSequences(board, r, c);
        if (jumpSequences.length) jumps.push(...jumpSequences);
        getMoves(board, r, c).forEach((move) => moves.push({
          from: [r, c],
          to: move.to,
          captured: move.captured,
          sequence: [{ from:[r, c], ...move }],
        }));
      }

  return jumps.length > 0 ? jumps : moves;
}

/** Apply a full turn, including all chained capture hops. */
function applyTurnMove(board, move) {
  const sequence = move.sequence || [move];
  return sequence.reduce((nextBoard, step) => applyMove(nextBoard, step), board);
}

/**
 * checkWinner — returns winning player number or null.
 * Win condition: opponent has no legal moves (captured all or fully blocked).
 */
function checkWinner(board, lastMover) {
  const opp = lastMover === 1 ? 2 : 1;
  return getAllTurnMoves(board, opp).length === 0 ? lastMover : null;
}

// ─────────────────────────────────────────────────────────────
// AI ENGINE — Minimax + Alpha-Beta Pruning
// CODEX: swap in MCTS for stronger/faster play at higher depths
// ─────────────────────────────────────────────────────────────

/**
 * evaluate — static board score from P2's perspective.
 * Positive = P2 (AI) advantage. Negative = P1 advantage.
 * Factors: piece count, king weight, advancement, edge safety.
 */
function evaluate(board) {
  let score = 0;
  for (let r = 0; r < 8; r++)
    for (let c = 0; c < 8; c++) {
      const p = board[r][c];
      const edge = (c === 0 || c === 7) ? 0.4 : 0; // edge pieces harder to capture
      if      (p === P2)  score += 10 + r * 0.4 + edge;   // P2 advances toward row 7
      else if (p === P2K) score += 18 + edge;
      else if (p === P1)  score -= 10 + (7-r) * 0.4 + edge; // P1 advances toward row 0
      else if (p === P1K) score -= 18 + edge;
    }
  return score;
}

/**
 * minimax — recursive α-β pruned game tree search.
 * maximizing = true → P2 (AI) is choosing; false → P1.
 */
function minimax(board, depth, alpha, beta, maximizing) {
  const player = maximizing ? 2 : 1;
  const moves  = getAllTurnMoves(board, player);
  if (moves.length === 0) return maximizing ? -Infinity : Infinity;
  if (depth === 0) return evaluate(board);
  if (maximizing) {
    let best = -Infinity;
    for (const m of moves) {
      best  = Math.max(best, minimax(applyTurnMove(board, m), depth-1, alpha, beta, false));
      alpha = Math.max(alpha, best);
      if (beta <= alpha) break; // β cut-off
    }
    return best;
  } else {
    let best = Infinity;
    for (const m of moves) {
      best = Math.min(best, minimax(applyTurnMove(board, m), depth-1, alpha, beta, true));
      beta = Math.min(beta, best);
      if (beta <= alpha) break; // α cut-off
    }
    return best;
  }
}

/** Return the best full-turn move for AI (P2). */
function getBestAIMove(board) {
  const moves = getAllTurnMoves(board, 2);
  if (!moves.length) return null;
  let best = -Infinity, bestMove = moves[0];
  for (const m of moves) {
    const val = minimax(applyTurnMove(board, m), AI_DEPTH-1, -Infinity, Infinity, false);
    if (val > best) { best = val; bestMove = m; }
  }
  return bestMove;
}

// ─────────────────────────────────────────────────────────────
// SUB-COMPONENTS
// ─────────────────────────────────────────────────────────────

/** CRT scanline texture overlay — purely decorative. */
function Scanlines() {
  return (
    <View style={S.scanlines} pointerEvents="none">
      {Array.from({ length: Math.ceil(SH / 4) }).map((_,i) => (
        <View key={i} style={S.scanline} />
      ))}
    </View>
  );
}

/**
 * GlitchText — title text with chromatic-aberration glitch effect.
 * Two offset ghost copies (magenta + lime) oscillate behind the main text.
 */
function GlitchText({ text, style }) {
  const off = useRef(new Animated.Value(0)).current;
  useEffect(() => {
    Animated.loop(
      Animated.sequence([
        Animated.timing(off, { toValue:  2.5, duration: 70,   useNativeDriver: true }),
        Animated.timing(off, { toValue: -2.0, duration: 70,   useNativeDriver: true }),
        Animated.timing(off, { toValue:  0,   duration: 2600, useNativeDriver: true }),
      ])
    ).start();
  }, []);
  const neg = Animated.multiply(off, -0.6);
  return (
    <View>
      <Animated.Text style={[style,{transform:[{translateX:off }],opacity:0.32,color:C.p1,position:"absolute"}]}>{text}</Animated.Text>
      <Animated.Text style={[style,{transform:[{translateX:neg }],opacity:0.32,color:C.p2,position:"absolute"}]}>{text}</Animated.Text>
      <Text style={style}>{text}</Text>
    </View>
  );
}

/**
 * Piece — a single checker disc with glow, king crown, and animation.
 *
 * isNew  → spring-bounce entrance animation
 * isSelected → pulsing glow loop
 * tiltCompensation → scaleY correction so circles stay circular under rotateX
 *
 * CODEX: replace with a proper 3-D mesh (Three.js/r3f) for true VR pieces.
 */
function Piece({ piece, isSelected, isNew, tiltCompensation }) {
  const scale = useRef(new Animated.Value(isNew ? 0.15 : 1)).current;
  const glow  = useRef(new Animated.Value(0)).current;

  useEffect(() => {
    if (isNew)
      Animated.spring(scale, { toValue:1, friction:5, tension:220, useNativeDriver:true }).start();
  }, [isNew]);

  useEffect(() => {
    if (isSelected) {
      Animated.loop(
        Animated.sequence([
          Animated.timing(glow, { toValue:1, duration:480, useNativeDriver:true }),
          Animated.timing(glow, { toValue:0, duration:480, useNativeDriver:true }),
        ])
      ).start();
    } else {
      glow.stopAnimation();
      glow.setValue(0);
    }
  }, [isSelected]);

  const p1    = isP1Piece(piece);
  const king  = isKing(piece);
  const color = p1 ? C.p1 : C.p2;
  const gClr  = p1 ? C.p1Glow : C.p2Glow;
  const kClr  = p1 ? C.p1King : C.p2King;
  const d     = CELL * 0.70;
  const glowOp = glow.interpolate({ inputRange:[0,1], outputRange:[0.28,1] });

  /**
   * Perspective correction:
   * rotateX(θ) squishes circles into ellipses. We undo it with
   * scaleY = 1/cos(θ) so pieces look round from the viewer's eye.
   * CODEX: remove this hack when upgrading to 3-D meshes.
   */
  const TILT_RAD = (TILT * Math.PI) / 180;
  const scaleYFix = tiltCompensation ? 1 / Math.cos(TILT_RAD) : 1;

  return (
    <Animated.View style={{ transform:[{scale},{scaleY:scaleYFix}] }}>
      <Animated.View style={[S.pieceGlow, {
        width:d+14, height:d+14, borderRadius:(d+14)/2,
        backgroundColor:gClr, opacity: isSelected ? glowOp : 0.22,
        shadowColor:color, shadowRadius: isSelected ? 18 : 7, shadowOpacity:1,
      }]}/>
      <View style={[S.piece,{
        width:d, height:d, borderRadius:d/2,
        backgroundColor:color,
        borderColor: isSelected ? C.cyan : king ? kClr : color,
        borderWidth: isSelected ? 2.5 : 1.5,
        shadowColor:color, shadowRadius:12, shadowOpacity:0.85,
      }]}>
        {king && <Text style={[S.kingIcon,{color:kClr,textShadowColor:kClr,fontSize:d*0.32}]}>♛</Text>}
        <View style={S.pieceShine}/>
      </View>
    </Animated.View>
  );
}

// ─────────────────────────────────────────────────────────────
// ROOT COMPONENT
// ─────────────────────────────────────────────────────────────

export default function NeonCheckers() {

  // ── Screen router ───────────────────────────────────────────
  // "menu" | "rules" | "game"
  const [screen, setScreen] = useState("menu");

  // ── Game mode ───────────────────────────────────────────────
  // "ai" | "local"
  const [mode, setMode] = useState(null);

  // ── Board & game state ──────────────────────────────────────
  const [board,          setBoard]          = useState(initBoard());
  const [currentPlayer,  setCurrentPlayer]  = useState(1);
  const [selected,       setSelected]       = useState(null);   // [r,c] | null
  const [validMoves,     setValidMoves]     = useState([]);
  const [mustJump,       setMustJump]       = useState(false);
  const [chainJump,      setChainJump]      = useState(null);   // [r,c] of chained piece
  const [winner,         setWinner]         = useState(null);
  const [lastMoved,      setLastMoved]      = useState(null);
  const [capturedBy1,    setCapturedBy1]    = useState(0);
  const [capturedBy2,    setCapturedBy2]    = useState(0);
  const [aiThinking,     setAiThinking]     = useState(false);
  const [quietTurns,     setQuietTurns]     = useState(0);
  const [moveLog,        setMoveLog]        = useState([]);

  // ── Animation refs ──────────────────────────────────────────
  /**
   * flipAnim: 0 = P1's view, 1 = P2's view.
   * Interpolated to 0°/180° rotation for board-flip in local mode.
   * CODEX: add gyroscope tilt on top of this via expo-sensors.
   */
  const flipAnim  = useRef(new Animated.Value(0)).current;
  const boardFade = useRef(new Animated.Value(0)).current;
  const winAnim   = useRef(new Animated.Value(0)).current;
  const aiTurnTimeoutRef = useRef(null);
  const aiTurnRequestRef = useRef(0);

  function cancelPendingAI() {
    aiTurnRequestRef.current += 1;
    if (aiTurnTimeoutRef.current) {
      clearTimeout(aiTurnTimeoutRef.current);
      aiTurnTimeoutRef.current = null;
    }
  }

  function getPlayerLogLabel(player) {
    return player === 2 && mode === "ai" ? "AI" : `P${player}`;
  }

  // Fade board in when game starts
  useEffect(() => {
    if (screen === "game") {
      boardFade.setValue(0);
      Animated.timing(boardFade, { toValue:1, duration:700, useNativeDriver:true }).start();
    }
  }, [screen]);

  // Board flip on turn change (local mode only)
  useEffect(() => {
    if (mode === "local" && screen === "game") {
      SFX.flip();
      Animated.spring(flipAnim, {
        toValue: currentPlayer === 1 ? 0 : 1,
        friction:6, tension:60, useNativeDriver:true,
      }).start();
    }
  }, [currentPlayer, mode]);

  useEffect(() => () => cancelPendingAI(), []);

  // AI move trigger
  useEffect(() => {
    cancelPendingAI();

    if (mode !== "ai" || currentPlayer !== 2 || winner || screen !== "game") {
      setAiThinking(false);
      return;
    }

    const requestId = aiTurnRequestRef.current;
    setAiThinking(true);

    // setTimeout keeps UI thread free while minimax runs synchronously.
    // CODEX: offload minimax to a Web Worker for true non-blocking.
    aiTurnTimeoutRef.current = setTimeout(() => {
      if (aiTurnRequestRef.current !== requestId) return;

      aiTurnTimeoutRef.current = null;
      const move = getBestAIMove(board);
      if (aiTurnRequestRef.current !== requestId) return;

      setAiThinking(false);
      if (move) executeTurn(board, move, 2);
    }, 650);

    return cancelPendingAI;
  }, [currentPlayer, mode, board, winner, screen]);

  // Win pulse animation
  useEffect(() => {
    if (winner) {
      SFX.win();
      Animated.loop(
        Animated.sequence([
          Animated.timing(winAnim, { toValue:1, duration:550, useNativeDriver:true }),
          Animated.timing(winAnim, { toValue:0, duration:550, useNativeDriver:true }),
        ])
      ).start();
    }
  }, [winner]);

  // ── Move execution ───────────────────────────────────────────

  /**
   * executeMove — apply a validated move to the board.
   *
   * Responsibilities:
   *   1. Apply the move via applyMove (pure)
   *   2. Play appropriate SFX
   *   3. Update capture counts
   *   4. Detect king promotion for SFX
   *   5. Check for chain-jump continuation (mandatory rule)
   *   6. Check for winner
   *   7. Swap turns and clear selection state
   *
   * @param {number[][]} currentBoard - board state before the move
   * @param {{ from:[r,c], to:[r,c], captured:[r,c]|null }} move
   * @param {1|2} player
   */
  function executeMove(currentBoard, move, player) {
    const nb = applyMove(currentBoard, move);
    const playerLabel = getPlayerLogLabel(player);

    // Detect promotion (was normal, now king)
    const promoted  = didPromote(currentBoard, nb, move, player);

    if (move.captured) {
      SFX.capture();
      if (player === 1) setCapturedBy1((n) => n + 1);
      else              setCapturedBy2((n) => n + 1);
    } else {
      SFX.move();
    }
    if (promoted) SFX.king();

    setLastMoved(move.to);
    setBoard(nb);

    // ── Chain-jump check ──────────────────────────────────────
    // RULE: if a capture just occurred and the same piece can jump again, it MUST.
    if (move.captured) {
      const chains = getJumps(nb, move.to[0], move.to[1]);
      if (chains.length > 0) {
        // Stay in chain — do NOT swap turns yet
        setChainJump(move.to);
        setSelected(move.to);
        setValidMoves(chains.map((j) => ({ from: move.to, ...j })));
        addLog(`${playerLabel} ⚡ chain jump!`);
        return;
      }
    }

    const label = promoted ? "👑 Kinged!" : move.captured ? "💥 Captured" : "→ Moved";
    finishTurn(nb, player, {
      capturedCount: move.captured ? 1 : 0,
      promoted,
      logLabel: `${playerLabel}: ${label}`,
    });
  }

  function executeTurn(currentBoard, turnMove, player) {
    const sequence = turnMove.sequence || [turnMove];
    const playerLabel = getPlayerLogLabel(player);
    let nb = currentBoard;
    let capturedCount = 0;
    let promoted = false;

    for (const step of sequence) {
      const nextBoard = applyMove(nb, step);
      promoted = promoted || didPromote(nb, nextBoard, step, player);
      if (step.captured) capturedCount += 1;
      nb = nextBoard;
    }

    if (capturedCount) {
      SFX.capture();
      if (player === 1) setCapturedBy1((n) => n + capturedCount);
      else              setCapturedBy2((n) => n + capturedCount);
    } else {
      SFX.move();
    }
    if (promoted) SFX.king();

    setLastMoved(turnMove.to);
    setBoard(nb);

    const label = promoted
      ? (capturedCount ? `👑 Kinged after ${capturedCount} capture${capturedCount > 1 ? "s" : ""}` : "👑 Kinged!")
      : capturedCount > 1 ? `💥 Captured x${capturedCount}`
      : capturedCount === 1 ? "💥 Captured"
      : "→ Moved";

    finishTurn(nb, player, {
      capturedCount,
      promoted,
      logLabel: `${playerLabel}: ${label}`,
    });
  }

  function finishTurn(finalBoard, player, { capturedCount = 0, promoted = false, logLabel }) {
    const nextQuietTurns = capturedCount > 0 || promoted ? 0 : quietTurns + 1;
    setQuietTurns(nextQuietTurns);

    const w = checkWinner(finalBoard, player);
    if (w) {
      setWinner(w);
      setSelected(null);
      setValidMoves([]);
      setChainJump(null);
      setMustJump(false);
      addLog(logLabel);
      return;
    }

    if (nextQuietTurns >= QUIET_TURN_DRAW_LIMIT) {
      setWinner("draw");
      setSelected(null);
      setValidMoves([]);
      setChainJump(null);
      setMustJump(false);
      addLog(logLabel);
      addLog(`DRAW: ${QUIET_TURN_DRAW_LIMIT} quiet turns`);
      return;
    }

    const nextPlayer = player === 1 ? 2 : 1;
    setCurrentPlayer(nextPlayer);
    setSelected(null);
    setValidMoves([]);
    setChainJump(null);
    setMustJump(false);
    addLog(logLabel);
  }

  function addLog(entry) {
    setMoveLog((l) => [...l.slice(-9), entry]);
  }

  // ── Cell press handler ───────────────────────────────────────

  /**
   * handleCellPress — interpret a tap on cell (r,c).
   *
   * State machine:
   *   1. Guard: ignore if AI turn, game over, or non-dark square
   *   2. Chain-jump mode: only allow the chained piece to move
   *   3. Select own piece → compute legal moves, enforce mandatory jump
   *   4. Tap a highlighted target → execute the move
   *   5. Tap elsewhere → deselect + invalid SFX
   */
  function handleCellPress(r, c) {
    if (winner || aiThinking) return;
    if (mode === "ai" && currentPlayer === 2) return;

    const piece = board[r][c];

    // ── State: chain jump in progress ─────────────────────────
    if (chainJump) {
      if (r === chainJump[0] && c === chainJump[1]) return; // tapped same piece
      const mv = validMoves.find((m) => m.to[0] === r && m.to[1] === c);
      if (mv) { executeMove(board, mv, currentPlayer); return; }
      SFX.invalid(); return;
    }

    // ── Select own piece ──────────────────────────────────────
    if (ownedBy(piece, currentPlayer)) {
      const allJumps    = getAllMoves(board, currentPlayer).filter((m) => m.captured);
      const hasMustJump = allJumps.length > 0;
      // Mandatory jump: if jumps exist, only show this piece's jumps
      const pieceMoves  = hasMustJump
        ? allJumps.filter((m) => m.from[0] === r && m.from[1] === c)
        : getAllMoves(board, currentPlayer).filter((m) => m.from[0] === r && m.from[1] === c);
      if (!pieceMoves.length) { SFX.invalid(); return; }
      SFX.select();
      setSelected([r, c]);
      setValidMoves(pieceMoves);
      setMustJump(hasMustJump);
      return;
    }

    // ── Move to highlighted target ────────────────────────────
    if (selected) {
      const mv = validMoves.find((m) => m.to[0] === r && m.to[1] === c);
      if (mv) { executeMove(board, mv, currentPlayer); return; }
    }

    // ── Deselect / invalid ────────────────────────────────────
    SFX.invalid();
    setSelected(null);
    setValidMoves([]);
  }

  // ── Reset ────────────────────────────────────────────────────

  function resetGame() {
    cancelPendingAI();
    setBoard(initBoard());
    setCurrentPlayer(1);
    setSelected(null);
    setValidMoves([]);
    setWinner(null);
    setLastMoved(null);
    setCapturedBy1(0);
    setCapturedBy2(0);
    setAiThinking(false);
    setChainJump(null);
    setMustJump(false);
    setQuietTurns(0);
    setMoveLog([]);
    flipAnim.setValue(0);
    winAnim.stopAnimation();
  }

  // ═════════════════════════════════════════════════════════════
  // RENDER — MENU SCREEN
  // ═════════════════════════════════════════════════════════════

  if (screen === "menu") {
    return (
      <View style={S.root}>
        <StatusBar barStyle="light-content" backgroundColor={C.bg} />
        <Scanlines />
        <SafeAreaView style={{ flex:1, justifyContent:"center" }}>
          <View style={S.menuInner}>

            <GlitchText text="NEON"     style={[S.logoMain,{color:C.p1,textShadowColor:C.p1}]}/>
            <GlitchText text="CHECKERS" style={[S.logoSub, {color:C.p2,textShadowColor:C.p2}]}/>
            <Text style={S.tagline}>// vr edition · next-gen board game</Text>

            <View style={S.divider}/>

            <TouchableOpacity style={[S.menuBtn,{borderColor:C.p1}]}
              onPress={() => { SFX.select(); setMode("local"); resetGame(); setScreen("game"); }}>
              <LinearGradient colors={["#FF2D7818","#FF2D7804"]} style={S.menuBtnGrad}>
                <Text style={S.menuBtnEmoji}>👥</Text>
                <View>
                  <Text style={[S.menuBtnTitle,{color:C.p1}]}>2 PLAYERS</Text>
                  <Text style={S.menuBtnSub}>local · board flips each turn</Text>
                </View>
              </LinearGradient>
            </TouchableOpacity>

            <TouchableOpacity style={[S.menuBtn,{borderColor:C.p2}]}
              onPress={() => { SFX.select(); setMode("ai"); resetGame(); setScreen("game"); }}>
              <LinearGradient colors={["#AAFF0018","#AAFF0004"]} style={S.menuBtnGrad}>
                <Text style={S.menuBtnEmoji}>🤖</Text>
                <View>
                  <Text style={[S.menuBtnTitle,{color:C.p2}]}>VS AI</Text>
                  <Text style={S.menuBtnSub}>minimax depth-{AI_DEPTH} · alpha-beta</Text>
                </View>
              </LinearGradient>
            </TouchableOpacity>

            <TouchableOpacity style={[S.menuBtn,{borderColor:C.violet}]}
              onPress={() => { SFX.select(); setScreen("rules"); }}>
              <LinearGradient colors={["#7B2FFF18","#7B2FFF04"]} style={S.menuBtnGrad}>
                <Text style={S.menuBtnEmoji}>📋</Text>
                <View>
                  <Text style={[S.menuBtnTitle,{color:C.violet}]}>RULES</Text>
                  <Text style={S.menuBtnSub}>traditional + neon variants</Text>
                </View>
              </LinearGradient>
            </TouchableOpacity>

            <View style={S.divider}/>

            {[{col:C.p1,label:"Player 1 — Hot Magenta  ·  moves toward row 0 (up)"},
              {col:C.p2,label:"Player 2 / AI — Electric Lime  ·  moves toward row 7 (down)"}]
              .map(({col,label}) => (
                <View key={label} style={[S.legendRow,{marginBottom:8}]}>
                  <View style={[S.legendDot,{backgroundColor:col,shadowColor:col}]}/>
                  <Text style={S.legendText}>{label}</Text>
                </View>
              ))}

          </View>
        </SafeAreaView>
      </View>
    );
  }

  // ═════════════════════════════════════════════════════════════
  // RENDER — RULES SCREEN
  // ═════════════════════════════════════════════════════════════

  if (screen === "rules") {
    const Section = ({ title, color=C.cyan, items }) => (
      <View style={S.ruleSection}>
        <Text style={[S.ruleSectionTitle,{color}]}>{title}</Text>
        {items.map((item,i) => (
          <View key={i} style={S.ruleRow}>
            <Text style={[S.ruleBullet,{color}]}>▸</Text>
            <Text style={S.ruleText}>{item}</Text>
          </View>
        ))}
      </View>
    );

    return (
      <View style={S.root}>
        <StatusBar barStyle="light-content" backgroundColor={C.bg} />
        <Scanlines/>
        <SafeAreaView style={{flex:1}}>
          <View style={S.rulesHeader}>
            <TouchableOpacity onPress={() => { SFX.select(); setScreen("menu"); }} style={S.backBtn}>
              <Text style={S.backBtnTxt}>← BACK</Text>
            </TouchableOpacity>
            <Text style={[S.rulesTitle,{color:C.violet}]}>RULES</Text>
            <View style={{width:60}}/>
          </View>
          <ScrollView contentContainerStyle={S.rulesScroll}>
            <Text style={[S.rulesVersion,{color:C.textDim}]}>{RULES.version}</Text>

            <Section title="THE BOARD" color={C.cyan} items={[
              RULES.board.description,
              `Each player starts with ${RULES.board.startingPieces} pieces.`,
              `Player 1 (Magenta): ${RULES.board.p1Rows}.`,
              `Player 2 (Lime/AI): ${RULES.board.p2Rows}.`,
              RULES.board.firstMove,
            ]}/>

            <Section title="MOVEMENT" color={C.p2} items={[
              RULES.movement.standard,
              RULES.movement.king,
              RULES.movement.promotion,
              RULES.movement.noPass,
            ]}/>

            <Section title="CAPTURING" color={C.p1} items={[
              RULES.capturing.howToJump,
              RULES.capturing.mandatory,
              RULES.capturing.choiceOfJump,
              RULES.capturing.multiJump,
              RULES.capturing.kingCapture,
              RULES.capturing.removal,
            ]}/>

            <Section title="WINNING" color={C.violet} items={[
              RULES.winning.captureAll,
              RULES.winning.blockAll,
              RULES.winning.draw,
            ]}/>

            <Section title="KEY RULES SUMMARY" color={C.cyan} items={RULES.keyRules}/>

            <Section title="⚡ NEON VARIANTS — coming soon" color={C.p2} items={RULES.neonVariants}/>

            <View style={{height:48}}/>
          </ScrollView>
        </SafeAreaView>
      </View>
    );
  }

  // ═════════════════════════════════════════════════════════════
  // RENDER — GAME SCREEN
  // ═════════════════════════════════════════════════════════════

  // O(1) lookup sets for cell rendering
  const validSet   = new Set(validMoves.map((m) => `${m.to[0]},${m.to[1]}`));
  const captureSet = new Set(validMoves.filter((m) => m.captured).map((m) => `${m.to[0]},${m.to[1]}`));

  // Live piece counts
  let p1Count = 0, p2Count = 0;
  for (let r = 0; r < 8; r++)
    for (let c = 0; c < 8; c++) {
      if (isP1Piece(board[r][c])) p1Count++;
      if (isP2Piece(board[r][c])) p2Count++;
    }

  const p2Label = mode === "ai" ? "A.I." : "PLAYER 2";
  const isDraw = winner === "draw";
  const winnerAccent = isDraw ? C.violet : winner === 1 ? C.p1 : C.p2;

  /**
   * VR board transform stack:
   *
   *   perspective(PERSPECTIVE)  — 3-D projection focal length
   *   rotateX(TILT°)            — tilt board away from viewer (VR camera angle)
   *   rotate(flipDeg)           — 0°/180° per-turn flip in local mode
   *
   * These are split across two nested Views because React Native
   * applies transforms in array order and combining them causes
   * the flip to fight the tilt on some devices.
   *
   * CODEX: replace this entire section with a Three.js/r3f <Canvas>
   * with OrbitControls, PBR materials, and proper 3-D piece meshes.
   */
  const flipDeg = flipAnim.interpolate({ inputRange:[0,1], outputRange:["0deg","180deg"] });

  return (
    <View style={S.root}>
      <StatusBar barStyle="light-content" backgroundColor={C.bg}/>
      <Scanlines/>

      <SafeAreaView style={{flex:1}}>
        <ScrollView contentContainerStyle={S.gameScroll} showsVerticalScrollIndicator={false}>

          {/* ── Header ──────────────────────────────────────────── */}
          <View style={S.gameHeader}>
            <TouchableOpacity style={S.backBtn}
              onPress={() => { SFX.select(); setScreen("menu"); resetGame(); }}>
              <Text style={S.backBtnTxt}>← MENU</Text>
            </TouchableOpacity>
            <GlitchText text="NEON CHECKERS" style={S.gameHeaderTitle}/>
            <TouchableOpacity style={S.backBtn}
              onPress={() => { SFX.select(); resetGame(); }}>
              <Text style={S.backBtnTxt}>↺ NEW</Text>
            </TouchableOpacity>
          </View>

          {/* ── Score Bar ────────────────────────────────────────── */}
          <View style={[S.scoreBar,{width: BOARD_SIZE+20}]}>

            <View style={[S.scoreCard,{borderColor:C.p1},
              currentPlayer===1&&!winner ? {shadowColor:C.p1,shadowRadius:10,shadowOpacity:0.6} : null]}>
              <LinearGradient
                colors={currentPlayer===1&&!winner ? ["#FF2D7830","#FF2D7806"] : ["transparent","transparent"]}
                style={S.scoreCardInner}>
                <Text style={[S.scoreLabel,{color:C.p1}]}>PLAYER 1</Text>
                <Text style={[S.scoreCount,{color:C.p1,textShadowColor:C.p1}]}>{p1Count}</Text>
                <Text style={S.scoreSub}>💀 {capturedBy1}</Text>
              </LinearGradient>
            </View>

            <View style={S.vsBox}>
              <Text style={S.vsText}>VS</Text>
              {aiThinking && <Text style={[S.thinkText,{color:C.p2}]}>thinking…</Text>}
            </View>

            <View style={[S.scoreCard,{borderColor:C.p2},
              currentPlayer===2&&!winner ? {shadowColor:C.p2,shadowRadius:10,shadowOpacity:0.6} : null]}>
              <LinearGradient
                colors={currentPlayer===2&&!winner ? ["#AAFF0030","#AAFF0006"] : ["transparent","transparent"]}
                style={S.scoreCardInner}>
                <Text style={[S.scoreLabel,{color:C.p2}]}>{p2Label}</Text>
                <Text style={[S.scoreCount,{color:C.p2,textShadowColor:C.p2}]}>{p2Count}</Text>
                <Text style={S.scoreSub}>💀 {capturedBy2}</Text>
              </LinearGradient>
            </View>

          </View>

          {/* ── VR Board ─────────────────────────────────────────── */}
          <Animated.View style={[S.vrOuter,{opacity:boardFade}]}>
            {/* Outer: perspective + rotateX (VR tilt — never changes) */}
            <View style={{
              transform:[
                {perspective: PERSPECTIVE},
                {rotateX: `${TILT}deg`},
              ],
            }}>
              {/* Inner: animated 0°/180° board flip (per-turn in local mode) */}
              <Animated.View style={{transform:[{rotate:flipDeg}]}}>

                <View style={[S.boardRim,{
                  shadowColor: currentPlayer===1 ? C.p1 : C.p2,
                  borderColor: (currentPlayer===1 ? C.p1 : C.p2)+"55",
                }]}>
                  {/* Active-player colored top edge */}
                  <LinearGradient
                    colors={[currentPlayer===1 ? C.p1 : C.p2,"transparent"]}
                    style={S.boardEdgeBar}
                  />

                  {/* 8×8 grid */}
                  <View style={[S.board,{width:BOARD_SIZE,height:BOARD_SIZE}]}>
                    {board.map((row,r) =>
                      row.map((cell,c) => {
                        const dark          = (r+c)%2===1;
                        const isValidTarget = validSet.has(`${r},${c}`);
                        const isCapHint     = captureSet.has(`${r},${c}`);
                        const isSelCell     = selected && selected[0]===r && selected[1]===c;
                        const isLast        = lastMoved && lastMoved[0]===r && lastMoved[1]===c;

                        return (
                          <TouchableOpacity
                            key={`${r}-${c}`}
                            onPress={() => dark && handleCellPress(r,c)}
                            activeOpacity={dark ? 0.80 : 1}
                            style={[
                              S.cell,
                              {width:CELL, height:CELL},
                              dark ? {
                                backgroundColor:
                                  isSelCell    ? "#00EEFF14" :
                                  isCapHint    ? C.captureFaint :
                                  isValidTarget? C.cyanFaint :
                                  isLast       ? "#FFFFFF07" :
                                                 C.boardDark,
                                borderWidth:  isValidTarget ? 0.8 : 0,
                                borderColor:  isCapHint ? C.p1 : C.cyan,
                              } : {backgroundColor:C.boardLight},
                            ]}
                          >
                            {/* Move-target dot */}
                            {dark && isValidTarget && cell===EMPTY && (
                              <View style={[S.moveDot,{
                                borderColor: isCapHint ? C.p1 : C.cyan,
                                shadowColor: isCapHint ? C.p1 : C.cyan,
                                width:CELL*0.26, height:CELL*0.26,
                                borderRadius:CELL*0.13,
                              }]}/>
                            )}

                            {/* Piece */}
                            {dark && cell!==EMPTY && (
                              <View style={S.pieceWrap}>
                                <Piece
                                  piece={cell}
                                  isSelected={!!isSelCell}
                                  isNew={!!isLast}
                                  tiltCompensation={true}
                                />
                              </View>
                            )}

                            {/* Column label on bottom row for orientation */}
                            {r===7 && dark && (
                              <Text style={S.coordLabel}>{c}</Text>
                            )}
                          </TouchableOpacity>
                        );
                      })
                    )}
                  </View>
                </View>

              </Animated.View>
            </View>
          </Animated.View>

          {/* ── Turn Banner ──────────────────────────────────────── */}
          <View style={S.turnBanner}>
            {!winner && (
              <Text style={[S.turnText,{
                color:           currentPlayer===1 ? C.p1 : C.p2,
                textShadowColor: currentPlayer===1 ? C.p1 : C.p2,
              }]}>
                {chainJump ? "🔗 CHAIN JUMP — KEEP GOING!" :
                 mustJump  ? "⚡ MUST JUMP!" :
                 `${currentPlayer===1 ? "PLAYER 1" : p2Label}'S TURN`}
              </Text>
            )}
          </View>

          {/* ── Move Log ─────────────────────────────────────────── */}
          <View style={[S.logBox,{width:BOARD_SIZE+20}]}>
            <Text style={S.logTitle}>// MOVE LOG</Text>
            {moveLog.length===0
              ? <Text style={[S.logEntry,{color:C.textDim}]}>game in progress…</Text>
              : moveLog.slice(-5).reverse().map((e,i) => (
                  <Text key={i} style={[S.logEntry,{opacity:1-i*0.18}]}>{e}</Text>
                ))
            }
          </View>

          <View style={{height:50}}/>
        </ScrollView>
      </SafeAreaView>

      {/* ── Win Modal ────────────────────────────────────────── */}
      <Modal visible={!!winner} transparent animationType="fade">
        <View style={S.modalOverlay}>
          <Animated.View style={[S.winModal,{
            opacity:winAnim.interpolate({inputRange:[0,1],outputRange:[0.88,1]}),
          }]}>
            <LinearGradient
              colors={isDraw ? ["#12061E","#050518"] : winner===1 ? ["#200010","#0A001A"] : ["#0A1A00","#050518"]}
              style={S.winModalInner}>

              <Text style={{fontSize:52,marginBottom:8}}>{isDraw ? "🤝" : "🏆"}</Text>

              <GlitchText
                text={isDraw
                  ? "DRAW GAME"
                  : winner===1
                  ? (mode==="ai" ? "YOU WIN!" : "P1 WINS!")
                  : (mode==="ai" ? "AI WINS!" : "P2 WINS!")}
                style={[S.winTitle,{
                  color:           winnerAccent,
                  textShadowColor: winnerAccent,
                }]}
              />

              <Text style={S.winSub}>
                {isDraw
                  ? `${QUIET_TURN_DRAW_LIMIT} consecutive turns without a capture or promotion`
                  : winner===1
                  ? `${p2Label} has no legal moves remaining`
                  : `PLAYER 1 has no legal moves remaining`}
              </Text>

              <View style={S.winStats}>
                <View style={S.winStatCol}>
                  <Text style={[S.winStatNum,{color:C.p1}]}>{capturedBy1}</Text>
                  <Text style={S.winStatLabel}>P1 captures</Text>
                </View>
                <View style={[S.winStatDivider,{backgroundColor:C.uiBorder}]}/>
                <View style={S.winStatCol}>
                  <Text style={[S.winStatNum,{color:C.p2}]}>{capturedBy2}</Text>
                  <Text style={S.winStatLabel}>P2 captures</Text>
                </View>
              </View>

              <TouchableOpacity
                style={[S.winBtn,{borderColor:winnerAccent}]}
                onPress={resetGame}>
                <LinearGradient
                  colors={isDraw ? ["#7B2FFF35","#7B2FFF08"] : winner===1 ? ["#FF2D7835","#FF2D7808"] : ["#AAFF0035","#AAFF0008"]}
                  style={S.winBtnInner}>
                  <Text style={[S.winBtnText,{color:winnerAccent}]}>↺  PLAY AGAIN</Text>
                </LinearGradient>
              </TouchableOpacity>

              <TouchableOpacity onPress={() => { setScreen("menu"); resetGame(); }}>
                <Text style={[S.winMenuLink,{color:C.textDim}]}>← back to menu</Text>
              </TouchableOpacity>

            </LinearGradient>
          </Animated.View>
        </View>
      </Modal>

    </View>
  );
}

// ─────────────────────────────────────────────────────────────
// STYLESHEET
// ─────────────────────────────────────────────────────────────

const FONT = Platform.OS === "ios" ? "Courier New" : "monospace";

const S = StyleSheet.create({
  // ── Base ──────────────────────────────────────────────────────
  root:      { flex:1, backgroundColor:C.bg },
  scanlines: { ...StyleSheet.absoluteFillObject, zIndex:999 },
  scanline:  { height:2, backgroundColor:C.scanline, marginBottom:2 },

  // ── Menu ──────────────────────────────────────────────────────
  menuInner:    { alignItems:"center", paddingHorizontal:28 },
  logoMain:     { fontSize:70, fontWeight:"900", letterSpacing:8,
                  textShadowRadius:22, textShadowOffset:{width:0,height:0}, fontFamily:FONT },
  logoSub:      { fontSize:34, fontWeight:"900", letterSpacing:12, marginTop:-6,
                  textShadowRadius:16, textShadowOffset:{width:0,height:0}, fontFamily:FONT },
  tagline:      { color:C.textDim, fontSize:11, letterSpacing:3, marginTop:6, fontFamily:FONT },
  divider:      { height:1, width:"100%", backgroundColor:C.uiBorder, marginVertical:24 },
  menuBtn:      { width:"100%", borderRadius:12, borderWidth:1, marginBottom:12, overflow:"hidden" },
  menuBtnGrad:  { flexDirection:"row", alignItems:"center", paddingVertical:15, paddingHorizontal:16, gap:14 },
  menuBtnEmoji: { fontSize:28 },
  menuBtnTitle: { fontSize:17, fontWeight:"800", letterSpacing:4, fontFamily:FONT },
  menuBtnSub:   { color:C.textDim, fontSize:10, letterSpacing:2, marginTop:2, fontFamily:FONT },
  legendRow:    { flexDirection:"row", alignItems:"center", gap:10 },
  legendDot:    { width:12, height:12, borderRadius:6,
                  shadowRadius:6, shadowOpacity:0.9, shadowOffset:{width:0,height:0} },
  legendText:   { color:C.textDim, fontSize:11, letterSpacing:1, fontFamily:FONT, flex:1 },

  // ── Rules ─────────────────────────────────────────────────────
  rulesHeader:      { flexDirection:"row", alignItems:"center", justifyContent:"space-between",
                      paddingHorizontal:12, paddingVertical:10,
                      borderBottomWidth:1, borderColor:C.uiBorder },
  rulesTitle:       { fontSize:14, fontWeight:"800", letterSpacing:4, fontFamily:FONT },
  rulesScroll:      { paddingHorizontal:16, paddingTop:16 },
  rulesVersion:     { fontSize:10, letterSpacing:2, marginBottom:18, fontFamily:FONT },
  ruleSection:      { marginBottom:22 },
  ruleSectionTitle: { fontSize:13, fontWeight:"800", letterSpacing:4, marginBottom:10, fontFamily:FONT },
  ruleRow:          { flexDirection:"row", gap:8, marginBottom:6 },
  ruleBullet:       { fontSize:11, marginTop:1, fontFamily:FONT },
  ruleText:         { flex:1, color:C.text, fontSize:12, lineHeight:18,
                      letterSpacing:0.4, fontFamily:FONT },

  // ── Game ──────────────────────────────────────────────────────
  gameScroll:      { alignItems:"center", paddingBottom:30 },
  gameHeader:      { flexDirection:"row", alignItems:"center", justifyContent:"space-between",
                     width:"100%", paddingHorizontal:10, paddingVertical:8 },
  gameHeaderTitle: { color:C.text, fontSize:13, fontWeight:"800", letterSpacing:3, fontFamily:FONT },
  backBtn:         { padding:8 },
  backBtnTxt:      { color:C.textDim, fontSize:11, letterSpacing:2, fontFamily:FONT },

  // Score
  scoreBar:      { flexDirection:"row", alignItems:"center", marginBottom:8, gap:8 },
  scoreCard:     { flex:1, borderRadius:10, borderWidth:1, overflow:"hidden",
                   shadowOffset:{width:0,height:0} },
  scoreCardInner:{ padding:10, alignItems:"center" },
  scoreLabel:    { fontSize:9, letterSpacing:2, fontWeight:"700", fontFamily:FONT },
  scoreCount:    { fontSize:30, fontWeight:"900",
                   textShadowRadius:10, textShadowOffset:{width:0,height:0}, fontFamily:FONT },
  scoreSub:      { color:C.textDim, fontSize:9, letterSpacing:1, marginTop:2, fontFamily:FONT },
  vsBox:         { alignItems:"center", width:48 },
  vsText:        { color:C.textDim, fontSize:11, fontWeight:"700", letterSpacing:2, fontFamily:FONT },
  thinkText:     { fontSize:8, letterSpacing:1, marginTop:3, fontFamily:FONT },

  // VR Board
  vrOuter:       { marginVertical:4 },
  boardRim:      { borderRadius:6, padding:4, backgroundColor:C.uiBorder,
                   shadowRadius:22, shadowOpacity:0.7, shadowOffset:{width:0,height:0},
                   borderWidth:1 },
  boardEdgeBar:  { height:3, borderRadius:2, marginBottom:3 },
  board:         { flexDirection:"row", flexWrap:"wrap" },
  cell:          { justifyContent:"center", alignItems:"center" },
  moveDot:       { borderWidth:2, shadowRadius:6, shadowOpacity:0.9,
                   shadowOffset:{width:0,height:0}, backgroundColor:"transparent" },
  pieceWrap:     { justifyContent:"center", alignItems:"center" },

  // Piece (used in <Piece />)
  pieceGlow:   { position:"absolute", shadowOffset:{width:0,height:0} },
  piece:       { justifyContent:"center", alignItems:"center",
                 shadowOffset:{width:0,height:0}, elevation:8 },
  pieceShine:  { position:"absolute", top:5, left:7, width:"38%", height:"32%",
                 borderRadius:20, borderWidth:1, borderColor:"rgba(255,255,255,0.28)" },
  kingIcon:    { fontWeight:"900", textShadowRadius:8, textShadowOffset:{width:0,height:0} },
  coordLabel:  { position:"absolute", bottom:1, right:2,
                 fontSize:7, color:C.textDim, fontFamily:FONT },

  // Turn banner
  turnBanner: { height:28, justifyContent:"center", marginTop:8 },
  turnText:   { fontSize:12, fontWeight:"800", letterSpacing:4,
                textShadowRadius:10, textShadowOffset:{width:0,height:0}, fontFamily:FONT },

  // Move log
  logBox:    { backgroundColor:C.uiBg, borderRadius:10, borderWidth:1,
               borderColor:C.uiBorder, padding:12, marginTop:8 },
  logTitle:  { color:C.textDim, fontSize:9, letterSpacing:3, marginBottom:6, fontFamily:FONT },
  logEntry:  { color:C.text, fontSize:11, letterSpacing:1, marginBottom:2, fontFamily:FONT },

  // Win modal
  modalOverlay:  { flex:1, backgroundColor:"#000000BB",
                   justifyContent:"center", alignItems:"center" },
  winModal:      { width:SW*0.88, borderRadius:20, overflow:"hidden",
                   borderWidth:1, borderColor:C.uiBorder },
  winModalInner: { padding:30, alignItems:"center" },
  winTitle:      { fontSize:40, fontWeight:"900", letterSpacing:6,
                   textShadowRadius:20, textShadowOffset:{width:0,height:0}, fontFamily:FONT },
  winSub:        { color:C.textDim, fontSize:11, letterSpacing:1,
                   marginTop:8, textAlign:"center", fontFamily:FONT },
  winStats:      { flexDirection:"row", marginVertical:20, gap:20 },
  winStatCol:    { alignItems:"center" },
  winStatNum:    { fontSize:32, fontWeight:"900", fontFamily:FONT },
  winStatLabel:  { color:C.textDim, fontSize:10, letterSpacing:1, fontFamily:FONT },
  winStatDivider:{ width:1, height:60 },
  winBtn:        { width:"100%", borderRadius:12, borderWidth:1, overflow:"hidden", marginBottom:14 },
  winBtnInner:   { paddingVertical:15, alignItems:"center" },
  winBtnText:    { fontSize:15, fontWeight:"800", letterSpacing:4, fontFamily:FONT },
  winMenuLink:   { fontSize:11, letterSpacing:2, fontFamily:FONT },
});
