'use client';

import { useState } from 'react';
import { getWorkflowStep } from '../lib/workflowSteps';
import { runWorkflowAction } from '../lib/workflowActions';

export function WorkflowDashboard({ steps }) {
  const [selectedStepId, setSelectedStepId] = useState(steps[0].id);
  const [copied, setCopied] = useState(false);
  const [isBusy, setIsBusy] = useState(false);
  const [statusMessage, setStatusMessage] = useState('Select a workflow step to review its prompt and integration stub.');
  const [lastResult, setLastResult] = useState(null);

  const selectedStep = getWorkflowStep(selectedStepId);
  const selectedIndex = steps.findIndex((step) => step.id === selectedStepId);

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
        ? `Calling the API placeholder for ${selectedStep.label}...`
        : `Queueing the backend placeholder for ${selectedStep.label}...`,
    );

    try {
      const result = await runWorkflowAction({ stepId: selectedStep.id, actionType });
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
        <section className="panel hero">
          <div className="hero-copy">
            <p className="eyebrow">Next.js Version</p>
            <h1>Ebook workflow, prompts, and backend seams in one responsive dashboard.</h1>
            <p className="hero-text">
              This version mirrors the plain React UI but routes placeholder actions through a Next.js API endpoint so
              you can expand into server actions, queues, or OpenAI calls later.
            </p>
          </div>

          <div className="hero-stats">
            <div className="stat-card">
              <strong>{steps.length}</strong>
              <span>workflow stages</span>
            </div>
            <div className="stat-card">
              <strong>1</strong>
              <span>API route stub</span>
            </div>
            <div className="stat-card">
              <strong>100%</strong>
              <span>data-driven layout</span>
            </div>
          </div>
        </section>

        <section className="dashboard-grid">
          <aside className="panel step-sidebar">
            <label className="field-card">
              <span className="field-label">Workflow Step</span>
              <select
                value={selectedStepId}
                onChange={(event) => setSelectedStepId(event.target.value)}
                className="workflow-select"
              >
                {steps.map((step) => (
                  <option key={step.id} value={step.id}>
                    {step.label}
                  </option>
                ))}
              </select>
            </label>

            <div className="pill-grid">
              {steps.map((step) => (
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
              Step {selectedIndex + 1} of {steps.length}
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

          <section className="panel prompt-panel">
            <div className="panel-heading">
              <div>
                <p className="eyebrow">AI Prompt</p>
                <h3>Ready to copy or wire into your API flow.</h3>
              </div>

              <div className="button-row">
                <button type="button" className="secondary-button" onClick={handleCopy}>
                  {copied ? 'Copied' : 'Copy prompt'}
                </button>
                <button
                  type="button"
                  className="primary-button"
                  onClick={() => handleRunAction('ai')}
                  disabled={isBusy}
                >
                  {isBusy ? 'Running...' : 'API placeholder'}
                </button>
                <button
                  type="button"
                  className="ghost-button"
                  onClick={() => handleRunAction('backend')}
                  disabled={isBusy}
                >
                  Queue backend
                </button>
              </div>
            </div>

            <textarea className="prompt-textarea" value={selectedStep.prompt} readOnly />
          </section>

          <section className="panel status-panel">
            <div className="panel-heading">
              <div>
                <p className="eyebrow">API Placeholder</p>
                <h3>Latest response payload</h3>
              </div>
            </div>

            <p className="status-message">{statusMessage}</p>

            {lastResult ? (
              <pre className="result-preview">{JSON.stringify(lastResult, null, 2)}</pre>
            ) : (
              <div className="empty-state">Call the placeholder endpoint to inspect the returned payload.</div>
            )}
          </section>
        </section>
      </main>
    </div>
  );
}
