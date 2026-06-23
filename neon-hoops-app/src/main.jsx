import React from "react";
import ReactDOM from "react-dom/client";
import NeonHoopz from "./App";

class AppErrorBoundary extends React.Component {
  constructor(props) {
    super(props);
    this.state = { hasError: false, message: "" };
  }

  static getDerivedStateFromError(error) {
    return { hasError: true, message: error?.message || "Unknown runtime error." };
  }

  componentDidCatch(error) {
    // Keep a console trail for debugging without breaking the whole app.
    console.error("NeonHoopz runtime error:", error);
  }

  render() {
    if (this.state.hasError) {
      return (
        <div
          style={{
            minHeight: "100vh",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            background: "radial-gradient(circle at 50% 20%, #16345b 0%, #081321 42%, #040a13 100%)",
            color: "#d8f2ff",
            padding: 20,
            fontFamily: "system-ui, sans-serif",
          }}
        >
          <div
            style={{
              maxWidth: 720,
              width: "100%",
              border: "2px solid #3ecfff",
              borderRadius: 10,
              padding: 20,
              background: "rgba(8,20,34,0.92)",
              boxShadow: "0 0 28px rgba(0, 210, 255, 0.2)",
            }}
          >
            <h1 style={{ margin: 0, fontSize: 26, letterSpacing: 1 }}>Neon Hoopz Loaded With Error</h1>
            <p style={{ marginTop: 10, opacity: 0.88 }}>
              A runtime issue was detected. Refresh once. If it persists, share this message:
            </p>
            <pre
              style={{
                marginTop: 12,
                padding: 12,
                borderRadius: 6,
                background: "rgba(0,0,0,0.3)",
                border: "1px solid rgba(120,220,255,0.25)",
                whiteSpace: "pre-wrap",
                wordBreak: "break-word",
              }}
            >
              {this.state.message}
            </pre>
          </div>
        </div>
      );
    }
    return this.props.children;
  }
}

ReactDOM.createRoot(document.getElementById("root")).render(
  <AppErrorBoundary>
    <NeonHoopz />
  </AppErrorBoundary>
);
