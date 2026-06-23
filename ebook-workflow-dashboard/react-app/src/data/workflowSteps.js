export const workflowSteps = [
  {
    id: 'research',
    label: 'Research',
    title: 'Research The Reader, Market, And Promise',
    description:
      'Clarify who the ebook is for, what problem it solves, and how your angle differs from competing books or lead magnets.',
    deliverable: 'Audience snapshot and positioning notes',
    automationHint: 'Ideal place to plug in web research, transcripts, or customer interviews.',
    prompt: `Act as a senior nonfiction ebook strategist.

Help me research an ebook before outlining it.

Focus on:
- the target reader and their top pain points
- what outcome they want most
- the strongest promise this ebook can make
- likely objections or points of confusion
- 3 to 5 competing angles already in the market

Return:
1. A reader profile
2. A pain-point list
3. A positioning statement
4. 5 recommended chapter themes
5. Open questions I should answer before outlining`,
  },
  {
    id: 'outline',
    label: 'Outline',
    title: 'Turn The Idea Into A Clear Chapter Map',
    description:
      'Shape the ebook into a sequence that feels logical, motivating, and easy for readers to follow from first page to final takeaway.',
    deliverable: 'Chapter-by-chapter outline',
    automationHint: 'Connect this step to prompt templates, Notion, or a document generator.',
    prompt: `You are helping me design the outline for an ebook.

Use the core idea, target reader, and promise to create a practical structure.

Please produce:
- a working title
- a one-paragraph summary
- 6 to 8 chapter titles
- the purpose of each chapter
- the key takeaway for the reader after each chapter
- a suggested introduction and conclusion approach

Keep the outline concise, persuasive, and organized so it can move directly into drafting.`,
  },
  {
    id: 'draft',
    label: 'Draft',
    title: 'Draft Clean First-Pass Chapters Quickly',
    description:
      'Generate usable copy without over-polishing so momentum stays high and the full manuscript exists early in the process.',
    deliverable: 'Readable first draft',
    automationHint: 'Swap the placeholder action for an OpenAI call that writes chapter sections.',
    prompt: `Act as a professional ebook ghostwriter.

Write a strong first draft for the selected chapter of my ebook.

Requirements:
- match a confident, practical, clear tone
- explain ideas in simple language
- include examples when helpful
- use short sections with readable subheadings
- avoid filler and repeated points
- end with a short action summary

Output:
1. Chapter title
2. Drafted chapter body
3. Suggested callout box or worksheet idea
4. Notes on what may still need supporting evidence or examples`,
  },
  {
    id: 'edit',
    label: 'Edit',
    title: 'Edit For Clarity, Flow, And Authority',
    description:
      'Tighten the draft, remove repetition, sharpen transitions, and make the final read feel more polished and trustworthy.',
    deliverable: 'Edited manuscript notes',
    automationHint: 'Great step for quality scoring, rewrite suggestions, or editor review workflows.',
    prompt: `Act as an experienced developmental editor for business and nonfiction ebooks.

Review the draft and improve it for:
- clarity
- structure
- pacing
- consistency of tone
- stronger transitions
- less repetition

Return:
1. A short editorial summary
2. A rewritten version of weak sections when needed
3. A list of lines that feel vague, redundant, or underdeveloped
4. Final recommendations before launch formatting`,
  },
  {
    id: 'launch',
    label: 'Launch',
    title: 'Prepare The Ebook For Release',
    description:
      'Turn the manuscript into a launch-ready asset with a final checklist for formatting, landing page copy, delivery, and internal approvals.',
    deliverable: 'Launch checklist and assets',
    automationHint: 'Later connect this step to file export, email automation, or publishing pipelines.',
    prompt: `You are my ebook launch manager.

Create a launch preparation checklist for this ebook.

Include:
- final quality checks
- cover and formatting items
- landing page copy needs
- email sequence needs
- download or delivery setup
- internal approval checkpoints
- analytics or conversion tracking considerations

Format the answer as a staged checklist with owners, dependencies, and launch-day priorities.`,
  },
  {
    id: 'marketing',
    label: 'Marketing',
    title: 'Build Ongoing Marketing Around The Ebook',
    description:
      'Expand the ebook into promotion ideas that can drive signups, audience growth, and follow-on offers after launch.',
    deliverable: 'Promotion plan and campaign ideas',
    automationHint: 'Good place to connect scheduling, CRM sync, or content repurposing tools.',
    prompt: `Act as a content marketing strategist.

Use this ebook as the center of a marketing plan.

Create:
- a launch-week promotion plan
- 10 content repurposing ideas
- 5 email campaign ideas
- 5 social post angles
- 3 lead magnet or upsell extensions
- the core messaging themes to repeat across channels

Keep the recommendations practical for a small team and organize them by priority.`,
  },
];

export function getWorkflowStep(stepId) {
  return workflowSteps.find((step) => step.id === stepId) ?? workflowSteps[0];
}
