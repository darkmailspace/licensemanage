import { create } from "zustand";
import { persist } from "zustand/middleware";
import { AuthUser } from "@/types";
import { apiClient } from "@/lib/api";

interface AuthState {
  user: AuthUser | null;
  isAuthenticated: boolean;
  requiresMfa: boolean;
  mfaEmail: string | null;
  setUser: (user: AuthUser | null) => void;
  setRequiresMfa: (required: boolean, email?: string) => void;
  login: (user: AuthUser, accessToken: string, refreshToken?: string) => void;
  logout: () => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      isAuthenticated: false,
      requiresMfa: false,
      mfaEmail: null,

      setUser: (user) =>
        set({ user, isAuthenticated: !!user }),

      setRequiresMfa: (required, email) =>
        set({ requiresMfa: required, mfaEmail: email || null }),

      login: (user, accessToken, refreshToken) => {
        apiClient.setTokens(accessToken, refreshToken);
        set({
          user,
          isAuthenticated: true,
          requiresMfa: false,
          mfaEmail: null,
        });
      },

      logout: () => {
        apiClient.clearTokens();
        set({
          user: null,
          isAuthenticated: false,
          requiresMfa: false,
          mfaEmail: null,
        });
      },
    }),
    {
      name: "lm-admin-auth",
      partialize: (state) => ({
        user: state.user,
        isAuthenticated: state.isAuthenticated,
      }),
    }
  )
);
