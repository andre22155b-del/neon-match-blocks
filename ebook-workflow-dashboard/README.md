# Ebook Workflow Dashboard

This workspace includes two versions of the same ebook workflow dashboard:

- `react-app`: a plain React + Vite version
- `next-app`: a Next.js App Router version with a placeholder API route

Each version includes:

- a dropdown with the workflow steps `Research`, `Outline`, `Draft`, `Edit`, `Launch`, and `Marketing`
- a step detail panel with a title and short description
- a copy-ready AI prompt for the selected step
- placeholder logic so you can later connect OpenAI calls or backend actions

## Run the plain React version

```bash
cd /Users/drewtobar/Documents/ebook-workflow-dashboard/react-app
npm install
npm run dev
```

## Run the Next.js version

```bash
cd /Users/drewtobar/Documents/ebook-workflow-dashboard/next-app
npm install
npm run dev
```

## Main extension points

- React step data: `/Users/drewtobar/Documents/ebook-workflow-dashboard/react-app/src/data/workflowSteps.js`
- React placeholder runtime: `/Users/drewtobar/Documents/ebook-workflow-dashboard/react-app/src/services/workflowRuntime.js`
- Next step data: `/Users/drewtobar/Documents/ebook-workflow-dashboard/next-app/lib/workflowSteps.js`
- Next placeholder API route: `/Users/drewtobar/Documents/ebook-workflow-dashboard/next-app/app/api/workflow/route.js`
