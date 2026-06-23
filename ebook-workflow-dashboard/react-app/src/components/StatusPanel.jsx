export function StatusPanel({ message, result }) {
  return (
    <section className="panel status-panel">
      <div className="panel-heading">
        <div>
          <p className="eyebrow">Integration Placeholder</p>
          <h3>Latest mock action response</h3>
        </div>
      </div>

      <p className="status-message">{message}</p>

      {result ? (
        <pre className="result-preview">{JSON.stringify(result, null, 2)}</pre>
      ) : (
        <div className="empty-state">Run one of the placeholder actions to preview the handoff payload.</div>
      )}
    </section>
  );
}
