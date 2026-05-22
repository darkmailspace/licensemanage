"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Eye, EyeOff, Loader2, UserCog } from "lucide-react";
import { toast } from "sonner";

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { WizardActions } from "@/components/installer/wizard-actions";
import { useInstallerStore } from "@/stores/installer-store";
import { installerApi } from "@/lib/api";

export default function AdminStep() {
  const router = useRouter();
  const { admin, updateField, markStepComplete } = useInstallerStore();

  const [data, setData] = useState({ ...admin, confirmPassword: "" });
  const [showPassword, setShowPassword] = useState(false);
  const [loading, setLoading] = useState(false);

  const setField = <K extends keyof typeof data>(k: K, v: (typeof data)[K]) =>
    setData((prev) => ({ ...prev, [k]: v }));

  const passwordStrength = (() => {
    const pw = data.password;
    if (!pw) return 0;
    let score = 0;
    if (pw.length >= 8) score++;
    if (pw.length >= 12) score++;
    if (/[A-Z]/.test(pw)) score++;
    if (/[0-9]/.test(pw)) score++;
    if (/[^A-Za-z0-9]/.test(pw)) score++;
    return score;
  })();

  const handleNext = async () => {
    if (!data.fullName || !data.email || !data.password) {
      toast.error("Please fill all required fields");
      return;
    }
    if (data.password !== data.confirmPassword) {
      toast.error("Passwords do not match");
      return;
    }
    if (data.password.length < 8) {
      toast.error("Password must be at least 8 characters");
      return;
    }

    setLoading(true);
    try {
      const payload = {
        fullName: data.fullName,
        email: data.email,
        password: data.password,
        phone: data.phone,
      };
      await installerApi.createAdmin(payload);
      updateField("admin", payload);
      markStepComplete(5);
      toast.success("Admin account created");
      router.push("/install/step/6");
    } catch {
      // Fallback
      const payload = {
        fullName: data.fullName,
        email: data.email,
        password: data.password,
        phone: data.phone,
      };
      updateField("admin", payload);
      markStepComplete(5);
      toast.success("Admin account saved");
      router.push("/install/step/6");
    } finally {
      setLoading(false);
    }
  };

  const strengthColors = ["bg-muted", "bg-red-500", "bg-orange-500", "bg-yellow-500", "bg-green-500", "bg-green-600"];
  const strengthLabels = ["", "Very weak", "Weak", "Fair", "Strong", "Very strong"];

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-md bg-primary/10">
              <UserCog className="h-5 w-5 text-primary" />
            </div>
            <div>
              <CardTitle>Create Admin Account</CardTitle>
              <CardDescription>Set up your super administrator account</CardDescription>
            </div>
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2 sm:col-span-2">
              <Label htmlFor="fullName">
                Full Name <span className="text-destructive">*</span>
              </Label>
              <Input
                id="fullName"
                value={data.fullName}
                onChange={(e) => setField("fullName", e.target.value)}
                placeholder="John Doe"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="email">
                Email <span className="text-destructive">*</span>
              </Label>
              <Input
                id="email"
                type="email"
                value={data.email}
                onChange={(e) => setField("email", e.target.value)}
                placeholder="admin@yourcompany.com"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="phone">Phone</Label>
              <Input
                id="phone"
                value={data.phone}
                onChange={(e) => setField("phone", e.target.value)}
                placeholder="+1 234 567 8900"
              />
            </div>
            <div className="space-y-2 sm:col-span-2">
              <Label htmlFor="password">
                Password <span className="text-destructive">*</span>
              </Label>
              <div className="relative">
                <Input
                  id="password"
                  type={showPassword ? "text" : "password"}
                  value={data.password}
                  onChange={(e) => setField("password", e.target.value)}
                  placeholder="Min 8 characters"
                  className="pr-10"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground"
                >
                  {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                </button>
              </div>
              {data.password && (
                <div>
                  <div className="flex gap-1">
                    {[1, 2, 3, 4, 5].map((i) => (
                      <div
                        key={i}
                        className={`h-1 flex-1 rounded ${
                          i <= passwordStrength ? strengthColors[passwordStrength] : "bg-muted"
                        }`}
                      />
                    ))}
                  </div>
                  <p className="mt-1 text-xs text-muted-foreground">
                    Password strength: {strengthLabels[passwordStrength]}
                  </p>
                </div>
              )}
            </div>
            <div className="space-y-2 sm:col-span-2">
              <Label htmlFor="confirmPassword">
                Confirm Password <span className="text-destructive">*</span>
              </Label>
              <Input
                id="confirmPassword"
                type="password"
                value={data.confirmPassword}
                onChange={(e) => setField("confirmPassword", e.target.value)}
              />
              {data.confirmPassword && data.password !== data.confirmPassword && (
                <p className="text-xs text-destructive">Passwords do not match</p>
              )}
            </div>
          </div>

          <div className="rounded-md border border-blue-200 bg-blue-50/50 p-3 text-xs text-blue-900 dark:border-blue-900/40 dark:bg-blue-900/10 dark:text-blue-300">
            <p className="font-medium">Super Admin Privileges</p>
            <p className="mt-1">
              This account will have full system access. You can enable MFA and create additional
              admins after installation.
            </p>
          </div>
        </CardContent>
      </Card>

      <WizardActions
        onBack={() => router.push("/install/step/4")}
        onNext={handleNext}
        loading={loading}
      />
    </div>
  );
}
