export function WorkflowSelect({ steps, value, onChange }) {
  return (
    <label className="field-card">
      <span className="field-label">Workflow Step</span>
      <select value={value} onChange={(event) => onChange(event.target.value)} className="workflow-select">
        {steps.map((step) => (
          <option key={step.id} value={step.id}>
            {step.label}
          </option>
        ))}
      </select>
    </label>
  );
}
