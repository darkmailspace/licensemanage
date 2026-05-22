import { create } from "zustand";
import { persist } from "zustand/middleware";
import { apiClient } from "@/lib/api";
import type { CustomerUser } from "@/types";

interface AuthState {
  user: CustomerUser | null;
  isAuthenticated: boolean;
  setUser: (user: CustomerUser | null) => void;
  login: (user: CustomerUser, accessToken: string) => void;
  logout: () => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      isAuthenticated: false,

      setUser: (user) => set({ user, isAuthenticated: !!user }),

      login: (user, accessToken) => {
        apiClient.setToken(accessToken);
        set({ user, isAuthenticated: true });
      },

      logout: () => {
        apiClient.clearToken();
        set({ user: null, isAuthenticated: false });
      },
    }),
    {
      name: "lm-client-auth",
      partialize: (s) => ({ user: s.user, isAuthenticated: s.isAuthenticated }),
    }
  )
);
