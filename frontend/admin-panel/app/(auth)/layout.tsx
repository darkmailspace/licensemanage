import { Shield } from "lucide-react";

export default function AuthLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="min-h-screen grid lg:grid-cols-2">
      {/* Left side - Branding */}
      <div className="relative hidden lg:flex bg-gradient-to-br from-primary via-primary to-blue-700 p-10 text-primary-foreground">
        <div className="relative z-20 flex items-center text-lg font-medium">
          <Shield className="mr-2 h-6 w-6" />
          License Manager
        </div>
        <div className="relative z-20 mt-auto">
          <blockquote className="space-y-4">
            <p className="text-2xl leading-relaxed">
              &ldquo;Enterprise-grade license management with military-grade security.
              RSA-4096 encryption, AES-256 protection, and complete lifecycle management.&rdquo;
            </p>
            <footer className="text-sm opacity-90">
              Production-Ready · Scalable · Secure
            </footer>
          </blockquote>
        </div>
        <div className="absolute inset-0 opacity-10">
          <div className="absolute inset-0 bg-[radial-gradient(ellipse_at_top_right,_var(--tw-gradient-stops))] from-white/30 via-transparent to-transparent" />
        </div>
      </div>

      {/* Right side - Auth form */}
      <div className="flex items-center justify-center p-6 lg:p-8">
        <div className="mx-auto w-full max-w-md">{children}</div>
      </div>
    </div>
  );
}
