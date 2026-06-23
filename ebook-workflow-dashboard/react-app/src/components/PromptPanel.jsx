export function PromptPanel({ prompt, onCopy, copied, onRunAction, isBusy }) {
  return (
    <section className="panel prompt-panel">
      <div className="panel-heading">
        <div>
          <p className="eyebrow">AI Prompt</p>
          <h3>Copy, tweak, or send it into your next automation step.</h3>
        </div>
        <div className="button-row">
          <button type="button" className="secondary-button" onClick={onCopy}>
            {copied ? 'Copied' : 'Copy prompt'}
          </button>
          <button type="button" className="primary-button" onClick={() => onRunAction('ai')} disabled={isBusy}>
            {isBusy ? 'Running...' : 'AI placeholder'}
          </button>
          <button type="button" className="ghost-button" onClick={() => onRunAction('backend')} disabled={isBusy}>
            Queue backend
          </button>
        </div>
      </div>

      <textarea className="prompt-textarea" value={prompt} readOnly />
    </section>
  );
}
