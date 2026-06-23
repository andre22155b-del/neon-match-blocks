import SwiftUI
import AppKit

enum AddemPalette: String, CaseIterable, Identifiable {
    case purple
    case indigo
    case blue
    case teal
    case mint
    case green
    case gold
    case orange
    case coral
    case red
    case pink

    var id: String { rawValue }

    var title: String {
        switch self {
        case .purple: return "Purple"
        case .indigo: return "Indigo"
        case .blue: return "Blue"
        case .teal: return "Teal"
        case .mint: return "Mint"
        case .green: return "Green"
        case .gold: return "Gold"
        case .orange: return "Orange"
        case .coral: return "Coral"
        case .red: return "Red"
        case .pink: return "Pink"
        }
    }

    var tint: Color {
        switch self {
        case .purple: return Color(red: 0.67, green: 0.56, blue: 0.93)
        case .indigo: return Color(red: 0.48, green: 0.54, blue: 0.92)
        case .blue: return Color(red: 0.41, green: 0.66, blue: 0.96)
        case .teal: return Color(red: 0.33, green: 0.75, blue: 0.80)
        case .mint: return Color(red: 0.38, green: 0.83, blue: 0.70)
        case .green: return Color(red: 0.42, green: 0.78, blue: 0.51)
        case .gold: return Color(red: 0.90, green: 0.73, blue: 0.36)
        case .orange: return Color(red: 0.95, green: 0.60, blue: 0.33)
        case .coral: return Color(red: 0.95, green: 0.52, blue: 0.47)
        case .red: return Color(red: 0.90, green: 0.40, blue: 0.45)
        case .pink: return Color(red: 0.91, green: 0.49, blue: 0.72)
        }
    }

    var strong: Color {
        switch self {
        case .purple: return Color(red: 0.46, green: 0.33, blue: 0.83)
        case .indigo: return Color(red: 0.34, green: 0.39, blue: 0.78)
        case .blue: return Color(red: 0.29, green: 0.49, blue: 0.84)
        case .teal: return Color(red: 0.22, green: 0.57, blue: 0.64)
        case .mint: return Color(red: 0.25, green: 0.62, blue: 0.54)
        case .green: return Color(red: 0.29, green: 0.58, blue: 0.36)
        case .gold: return Color(red: 0.73, green: 0.52, blue: 0.22)
        case .orange: return Color(red: 0.78, green: 0.40, blue: 0.20)
        case .coral: return Color(red: 0.79, green: 0.33, blue: 0.28)
        case .red: return Color(red: 0.73, green: 0.25, blue: 0.31)
        case .pink: return Color(red: 0.74, green: 0.30, blue: 0.59)
        }
    }

    var panelTop: Color {
        tint.opacity(0.28).blend(with: .white, amount: 0.72)
    }

    var panelBottom: Color {
        tint.opacity(0.36).blend(with: .white, amount: 0.64)
    }
}

enum AddemTheme {
    static let paletteKey = AppGroupConfig.themePaletteKey
    static var textPrimary: Color { Color.primary.opacity(0.92) }
    static var textSecondary: Color { Color.primary.opacity(0.68) }

    static var sharedDefaults: UserDefaults {
        UserDefaults(suiteName: AppGroupConfig.groupID) ?? .standard
    }

    static func currentPalette(from defaults: UserDefaults? = nil) -> AddemPalette {
        let source = defaults ?? sharedDefaults
        let raw = source.string(forKey: paletteKey) ?? AddemPalette.purple.rawValue
        return AddemPalette(rawValue: raw) ?? .purple
    }

    static var purpleTint: Color { currentPalette().tint }
    static var purpleStrong: Color { currentPalette().strong }
    static var panelTop: Color { currentPalette().panelTop }
    static var panelBottom: Color { currentPalette().panelBottom }

    static var panelGradient: LinearGradient {
        LinearGradient(
            colors: [panelTop.opacity(0.84), panelBottom.opacity(0.74)],
            startPoint: .topLeading,
            endPoint: .bottomTrailing
        )
    }

    static func canvasGradient(for colorScheme: ColorScheme) -> LinearGradient {
        let palette = currentPalette()
        if colorScheme == .dark {
            let top = palette.strong.blend(with: .black, amount: 0.60).opacity(0.96)
            let mid = palette.tint.blend(with: Color(red: 0.12, green: 0.11, blue: 0.18), amount: 0.56).opacity(0.90)
            let bottom = Color(red: 0.08, green: 0.08, blue: 0.12).blend(with: palette.tint, amount: 0.16)
            return LinearGradient(colors: [top, mid, bottom], startPoint: .topLeading, endPoint: .bottomTrailing)
        }

        let top = palette.tint.blend(with: .white, amount: 0.78)
        let mid = palette.panelTop.blend(with: .white, amount: 0.66)
        let bottom = palette.panelBottom.blend(with: Color(red: 0.96, green: 0.97, blue: 1.0), amount: 0.64)
        return LinearGradient(colors: [top, mid, bottom], startPoint: .topLeading, endPoint: .bottomTrailing)
    }

    static func canvasGlowPrimary(for colorScheme: ColorScheme) -> Color {
        colorScheme == .dark ? purpleTint.opacity(0.30) : purpleTint.opacity(0.34)
    }

    static func canvasGlowSecondary(for colorScheme: ColorScheme) -> Color {
        colorScheme == .dark ? purpleStrong.opacity(0.28) : purpleStrong.opacity(0.24)
    }

    static func canvasGlowTertiary(for colorScheme: ColorScheme) -> Color {
        colorScheme == .dark ? Color.white.opacity(0.08) : Color.white.opacity(0.28)
    }

    static func glassBaseGradient(for colorScheme: ColorScheme) -> LinearGradient {
        if colorScheme == .dark {
            return LinearGradient(
                colors: [
                    Color.white.opacity(0.20),
                    purpleTint.opacity(0.20),
                    Color.black.opacity(0.12)
                ],
                startPoint: .topLeading,
                endPoint: .bottomTrailing
            )
        }

        return LinearGradient(
            colors: [
                Color.white.opacity(0.54),
                purpleTint.opacity(0.30),
                Color.white.opacity(0.24)
            ],
            startPoint: .topLeading,
            endPoint: .bottomTrailing
        )
    }

    static func glassSheenGradient(for colorScheme: ColorScheme) -> LinearGradient {
        LinearGradient(
            colors: [
                Color.white.opacity(colorScheme == .dark ? 0.26 : 0.46),
                Color.white.opacity(0.08),
                Color.clear
            ],
            startPoint: .topLeading,
            endPoint: .bottomTrailing
        )
    }

    static func glassBorder(for colorScheme: ColorScheme) -> Color {
        colorScheme == .dark ? Color.white.opacity(0.52) : Color.white.opacity(0.76)
    }

    static func glassInnerBorder(for colorScheme: ColorScheme) -> Color {
        colorScheme == .dark ? Color.white.opacity(0.22) : Color.white.opacity(0.42)
    }

    static func surfaceGlow(for colorScheme: ColorScheme) -> Color {
        colorScheme == .dark ? purpleTint.opacity(0.20) : purpleTint.opacity(0.22)
    }

    static func inputFill(for colorScheme: ColorScheme) -> Color {
        colorScheme == .dark ? Color.white.opacity(0.15) : Color.white.opacity(0.58)
    }

    static func inputBorder(for colorScheme: ColorScheme) -> Color {
        colorScheme == .dark ? Color.white.opacity(0.40) : Color.white.opacity(0.72)
    }

    static func chipFill(isActive: Bool, for colorScheme: ColorScheme) -> Color {
        if isActive {
            return colorScheme == .dark ? purpleTint.opacity(0.42) : purpleTint.opacity(0.36)
        }
        return colorScheme == .dark ? Color.white.opacity(0.16) : Color.white.opacity(0.70)
    }
}

struct GlassCard: ViewModifier {
    @Environment(\.colorScheme) private var colorScheme

    func body(content: Content) -> some View {
        let shape = RoundedRectangle(cornerRadius: 16, style: .continuous)

        content
            .padding(12)
            .background {
                shape
                    .fill(AddemTheme.glassBaseGradient(for: colorScheme))
                    .overlay(shape.fill(.ultraThinMaterial).opacity(0.92))
                    .overlay(shape.fill(AddemTheme.glassSheenGradient(for: colorScheme)))
                    .overlay(shape.stroke(AddemTheme.glassBorder(for: colorScheme), lineWidth: 1))
                    .overlay(shape.stroke(AddemTheme.glassInnerBorder(for: colorScheme), lineWidth: 0.6))
            }
            .shadow(color: AddemTheme.surfaceGlow(for: colorScheme), radius: 22, x: 0, y: 8)
            .shadow(color: .black.opacity(colorScheme == .dark ? 0.24 : 0.10), radius: 12, x: 0, y: 6)
    }
}

extension View {
    func glassCard() -> some View {
        modifier(GlassCard())
    }

    func glassInputField(cornerRadius: CGFloat = 12) -> some View {
        modifier(GlassInputField(cornerRadius: cornerRadius))
    }
}

struct GlassInputField: ViewModifier {
    @Environment(\.colorScheme) private var colorScheme
    let cornerRadius: CGFloat

    func body(content: Content) -> some View {
        let shape = RoundedRectangle(cornerRadius: cornerRadius, style: .continuous)

        content
            .background {
                shape
                    .fill(AddemTheme.inputFill(for: colorScheme))
                    .overlay(shape.fill(.thinMaterial).opacity(0.80))
                    .overlay(shape.stroke(AddemTheme.inputBorder(for: colorScheme), lineWidth: 1))
            }
    }
}

struct AddemLiquidBackground: View {
    @Environment(\.colorScheme) private var colorScheme
    @State private var driftPhase: CGFloat = -1
    @State private var pointer: CGPoint = CGPoint(x: 0.5, y: 0.5)
    @State private var isPointerActive = false

    var body: some View {
        GeometryReader { proxy in
            let width = max(proxy.size.width, 1)
            let height = max(proxy.size.height, 1)
            let primaryParallax = parallaxOffset(size: proxy.size, strength: 30)
            let secondaryParallax = parallaxOffset(size: proxy.size, strength: 22)
            let tertiaryParallax = parallaxOffset(size: proxy.size, strength: 16)
            let driftX = width * 0.06 * driftPhase
            let driftY = height * 0.05 * driftPhase

            Group {
                ZStack {
                    AddemTheme.canvasGradient(for: colorScheme)
                        .overlay(
                            LinearGradient(
                                colors: [
                                    Color.white.opacity(colorScheme == .dark ? 0.10 : 0.24),
                                    Color.clear,
                                    AddemTheme.purpleTint.opacity(colorScheme == .dark ? 0.10 : 0.18)
                                ],
                                startPoint: driftPhase > 0 ? .topLeading : .topTrailing,
                                endPoint: driftPhase > 0 ? .bottomTrailing : .bottomLeading
                            )
                            .blendMode(.screen)
                        )

                    Circle()
                        .fill(AddemTheme.canvasGlowPrimary(for: colorScheme))
                        .frame(width: max(width * 0.66, 360), height: max(width * 0.66, 360))
                        .blur(radius: 120)
                        .offset(
                            x: -width * 0.24 + driftX + primaryParallax.width,
                            y: -height * 0.30 + driftY * 0.65 + primaryParallax.height
                        )

                    Circle()
                        .fill(AddemTheme.canvasGlowSecondary(for: colorScheme))
                        .frame(width: max(width * 0.62, 340), height: max(width * 0.62, 340))
                        .blur(radius: 130)
                        .offset(
                            x: width * 0.30 - driftX * 0.72 - secondaryParallax.width,
                            y: height * 0.18 - driftY * 0.48 - secondaryParallax.height
                        )

                    Circle()
                        .fill(AddemTheme.canvasGlowTertiary(for: colorScheme))
                        .frame(width: max(width * 0.48, 260), height: max(width * 0.48, 260))
                        .blur(radius: 88)
                        .offset(
                            x: width * 0.10 + driftX * 0.42 + tertiaryParallax.width,
                            y: -height * 0.20 - driftY * 0.30 + tertiaryParallax.height
                        )
                }
                .animation(.easeInOut(duration: 0.55), value: pointer)
                .animation(.easeInOut(duration: 0.55), value: isPointerActive)
                .ignoresSafeArea()
            }
            #if os(macOS)
            .onContinuousHover { phase in
                switch phase {
                case .active(let location):
                    updatePointer(location: location, size: proxy.size)
                case .ended:
                    withAnimation(.easeOut(duration: 0.55)) {
                        isPointerActive = false
                        pointer = CGPoint(x: 0.5, y: 0.5)
                    }
                }
            }
            #endif
        }
        .onAppear {
            guard driftPhase < 0 else { return }
            withAnimation(.easeInOut(duration: 24).repeatForever(autoreverses: true)) {
                driftPhase = 1
            }
        }
        .allowsHitTesting(false)
    }

    private func parallaxOffset(size: CGSize, strength: CGFloat) -> CGSize {
        let nx = (pointer.x - 0.5) * 2
        let ny = (pointer.y - 0.5) * 2
        let damp = isPointerActive ? 1.0 : 0.28

        return CGSize(
            width: nx * strength * damp,
            height: ny * strength * damp
        )
    }

    private func updatePointer(location: CGPoint, size: CGSize) {
        guard size.width > 0, size.height > 0 else { return }

        let normalizedX = min(max(location.x / size.width, 0), 1)
        let normalizedY = min(max(location.y / size.height, 0), 1)

        withAnimation(.easeOut(duration: 0.25)) {
            isPointerActive = true
            pointer = CGPoint(x: normalizedX, y: normalizedY)
        }
    }
}

private extension Color {
    func blend(with other: Color, amount: CGFloat) -> Color {
        let clamped = min(max(amount, 0), 1)
        return Color(
            red: component(0) * (1 - clamped) + other.component(0) * clamped,
            green: component(1) * (1 - clamped) + other.component(1) * clamped,
            blue: component(2) * (1 - clamped) + other.component(2) * clamped
        )
    }

    func component(_ index: Int) -> Double {
        let ui = NSColor(self).usingColorSpace(.deviceRGB) ?? .white
        switch index {
        case 0: return Double(ui.redComponent)
        case 1: return Double(ui.greenComponent)
        default: return Double(ui.blueComponent)
        }
    }
}
