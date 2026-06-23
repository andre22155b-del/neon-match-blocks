import { WorkflowDashboard } from '../components/WorkflowDashboard';
import { workflowSteps } from '../lib/workflowSteps';

export default function Page() {
  return <WorkflowDashboard steps={workflowSteps} />;
}
