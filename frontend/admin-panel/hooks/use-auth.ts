"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuthStore } from "@/stores/auth-store";
import { authApi, apiClient } from "@/lib/api";

export function useAuth() {
  const router = useRouter();
  const { user, isAuthenticated, login, logout, setUser } = useAuthStore();

  useEffect(() => {
    const fetchUser = async () => {
      if (apiClient.isAuthenticated() && !user) {
        try {
          const response = await authApi.me();
          // eslint-disable-next-line @typescript-eslint/no-explicit-any
          const userData = (response as any)?.data;
          if (userData) {
            setUser(userData);
          }
        } catch {
          logout();
        }
      }
    };

    fetchUser();
  }, [user, setUser, logout]);

  const handleLogout = async () => {
    try {
      await authApi.logout();
    } catch {
      // ignore errors during logout
    } finally {
      logout();
      router.push("/login");
    }
  };

  return {
    user,
    isAuthenticated,
    login,
    logout: handleLogout,
  };
}
