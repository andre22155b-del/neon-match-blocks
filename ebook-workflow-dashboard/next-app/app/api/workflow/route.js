import { NextResponse } from 'next/server';
import { getWorkflowStep } from '../../../lib/workflowSteps';

export async function POST(request) {
  const body = await request.json();
  const step = getWorkflowStep(body.stepId);

  if (!step) {
    return NextResponse.json({ ok: false, message: 'Unknown workflow step.' }, { status: 404 });
  }

  const actionType = body.actionType === 'backend' ? 'backend' : 'ai';

  return NextResponse.json({
    ok: true,
    actionType,
    stepId: step.id,
    message:
      actionType === 'ai'
        ? `API placeholder completed for ${step.label}. Replace this response with a real OpenAI call when ready.`
        : `Backend placeholder accepted for ${step.label}. Replace this route with your job or queue integration.`,
    payload:
      actionType === 'ai'
        ? {
            provider: 'openai',
            model: 'replace-with-your-model',
            input: step.prompt,
          }
        : {
            endpoint: '/api/workflow',
            method: 'POST',
            body: {
              stepId: step.id,
              prompt: step.prompt,
            },
          },
  });
}
