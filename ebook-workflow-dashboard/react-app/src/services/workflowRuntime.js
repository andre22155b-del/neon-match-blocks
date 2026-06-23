const wait = (ms) => new Promise((resolve) => window.setTimeout(resolve, ms));

export async function runPlaceholderAction({ actionType, step }) {
  if (!step) {
    throw new Error('A workflow step is required.');
  }

  const startedAt = new Date().toISOString();
  await wait(actionType === 'ai' ? 800 : 450);

  if (actionType === 'ai') {
    return {
      ok: true,
      actionType,
      stepId: step.id,
      startedAt,
      finishedAt: new Date().toISOString(),
      message: `AI placeholder completed for ${step.label}. Replace this mock with an OpenAI request when you are ready.`,
      payload: {
        provider: 'openai',
        model: 'replace-with-your-model',
        input: step.prompt,
      },
    };
  }

  return {
    ok: true,
    actionType,
    stepId: step.id,
    startedAt,
    finishedAt: new Date().toISOString(),
    message: `Backend placeholder queued for ${step.label}. Swap this with a fetch call to your API or job runner.`,
    payload: {
      endpoint: '/api/workflow',
      method: 'POST',
      body: {
        stepId: step.id,
        prompt: step.prompt,
      },
    },
  };
}
