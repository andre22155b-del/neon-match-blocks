import { useState } from 'react';
import { PromptPanel } from './components/PromptPanel';
import { StatusPanel } from './components/StatusPanel';
import { WorkflowSelect } from './components/WorkflowSelect';
import { getWorkflowStep, workflowSteps } from './data/workflowSteps';
import { runPlaceholderAction } from './services/workflowRuntime';

export default function App() {
  const [selectedStepId, setSelectedStepId] = useState(workflowSteps[0].id);
  const [copied, setCopied] = useState(false);
  const [isBusy, setIsBusy] = useState(false);
  const [statusMessage, setStatusMessage] = useState('Choose a workflow step and copy its prompt whenever you are ready.');
  const [lastResult, setLastResult] = useState(null);

  const selectedStep = getWorkflowStep(selectedStepId);
  const selectedIndex = workflowSteps.findIndex((step) => step.id === selectedStepId);

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(selectedStep.prompt);
      setCopied(true);
      setStatusMessage(`Prompt copied for ${selectedStep.label}.`);
      window.setTimeout(() => setCopied(false), 1500);
    } catch (error) {
      setStatusMessage(`Clipboard access failed: ${error.message}`);
    }
  };

  const handleRunAction = async (actionType) => {
    setIsBusy(true);
    setStatusMessage(
      actionType === 'ai'
        ? `Running the AI placeholder for ${selectedStep.label}...`
        : `Queueing the backend placeholder for ${selectedStep.label}...`,
    );

    try {
      const result = await runPlaceholderAction({ actionType, step: selectedStep });
      setLastResult(result);
      setStatusMessage(result.message);
    } catch (error) {
      setStatusMessage(error.message);
    } finally {
      setIsBusy(false);
    }
  };

  return (
    <div className="app-shell">
      <main className="dashboard">
        <section className="hero panel">
          <div className="hero-copy">
            <p className="eyebrow">Ebook Workflow Dashboard</p>
            <h1>Move from research to marketing in one clean command center.</h1>
            <p className="hero-text">
              Select a stage, review the guidance, copy the prompt, and test the placeholder hooks you can later
              replace with OpenAI requests or backend jobs.
            </p>
          </div>

          <div className="hero-stats">
            <div className="stat-card">
              <strong>{workflowSteps.length}</strong>
              <span>workflow stages</span>
            </div>
            <div className="stat-card">
              <strong>2</strong>
              <span>integration stubs</span>
            </div>
            <div className="stat-card">
              <strong>1</strong>
              <span>copy-ready prompt panel</span>
            </div>
          </div>
        </section>

        <section className="dashboard-grid">
          <aside className="panel step-sidebar">
            <WorkflowSelect steps={workflowSteps} value={selectedStepId} onChange={setSelectedStepId} />

            <div className="pill-grid">
              {workflowSteps.map((step) => (
                <button
                  key={step.id}
                  type="button"
                  className={`step-pill ${step.id === selectedStepId ? 'active' : ''}`}
                  onClick={() => setSelectedStepId(step.id)}
                >
                  {step.label}
                </button>
              ))}
            </div>
          </aside>

          <section className="panel detail-panel">
            <p className="step-counter">
              Step {selectedIndex + 1} of {workflowSteps.length}
            </p>
            <h2>{selectedStep.title}</h2>
            <p className="detail-description">{selectedStep.description}</p>

            <div className="detail-grid">
              <article className="detail-card">
                <span className="detail-label">Primary deliverable</span>
                <strong>{selectedStep.deliverable}</strong>
              </article>
              <article className="detail-card">
                <span className="detail-label">Future integration</span>
                <strong>{selectedStep.automationHint}</strong>
              </article>
            </div>
          </section>

          <PromptPanel
            prompt={selectedStep.prompt}
            onCopy={handleCopy}
            copied={copied}
            onRunAction={handleRunAction}
            isBusy={isBusy}
          />

          <StatusPanel message={statusMessage} result={lastResult} />
        </section>
      </main>
    </div>
  );
}
