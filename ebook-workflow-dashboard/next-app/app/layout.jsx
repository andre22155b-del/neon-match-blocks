import './globals.css';

export const metadata = {
  title: 'Ebook Workflow Dashboard',
  description: 'A clean React and Next.js dashboard for managing ebook workflow prompts and actions.',
};

export default function RootLayout({ children }) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
