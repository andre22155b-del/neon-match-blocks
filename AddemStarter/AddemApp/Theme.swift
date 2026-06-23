import SwiftUI

enum AddemTheme {
    static let purpleTint = Color(red: 0.67, green: 0.56, blue: 0.93)
    static let panelTop = Color(red: 0.95, green: 0.91, blue: 1.0)
    static let panelBottom = Color(red: 0.90, green: 0.85, blue: 0.98)

    static let panelGradient = LinearGradient(
        colors: [panelTop.opacity(0.78), panelBottom.opacity(0.66)],
        startPoint: .topLeading,
        endPoint: .bottomTrailing
    )
}

struct GlassCard: ViewModifier {
    func body(content: Content) -> some View {
        content
            .padding(12)
            .background(AddemTheme.panelGradient)
            .background(.ultraThinMaterial)
            .clipShape(RoundedRectangle(cornerRadius: 16, style: .continuous))
            .overlay(
                RoundedRectangle(cornerRadius: 16, style: .continuous)
                    .stroke(Color.white.opacity(0.55), lineWidth: 1)
            )
            .shadow(color: .black.opacity(0.08), radius: 10, x: 0, y: 6)
    }
}

extension View {
    func glassCard() -> some View {
        modifier(GlassCard())
    }
}
