export async function runWorkflowAction({ stepId, actionType }) {
  const response = await fetch('/api/workflow', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      stepId,
      actionType,
    }),
  });

  const result = await response.json();

  if (!response.ok) {
    throw new Error(result.message || 'Workflow request failed.');
  }

  return result;
}
