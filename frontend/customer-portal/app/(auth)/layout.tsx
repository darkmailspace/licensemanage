import { Shield } from "lucide-react";

export default function AuthLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="grid min-h-screen lg:grid-cols-2">
      <div className="relative hidden lg:flex bg-gradient-to-br from-blue-600 via-blue-700 to-indigo-800 p-10 text-white">
        <div className="relative z-20 flex items-center text-lg font-medium">
          <Shield className="mr-2 h-6 w-6" />
          Customer Portal
        </div>
        <div className="relative z-20 mt-auto">
          <blockquote className="space-y-4">
            <p className="text-2xl leading-relaxed">
              &ldquo;Manage your licenses, renew subscriptions, download updates,
              and get support — all in one place.&rdquo;
            </p>
            <footer className="text-sm opacity-90">
              Your self-service license hub
            </footer>
          </blockquote>
        </div>
      </div>

      <div className="flex items-center justify-center p-6 lg:p-8">
        <div className="mx-auto w-full max-w-md">{children}</div>
      </div>
    </div>
  );
}
