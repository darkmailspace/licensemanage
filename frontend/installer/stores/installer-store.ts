import { create } from "zustand";
import { persist } from "zustand/middleware";

export interface InstallerData {
  // Step 1: License
  licenseKey: string;
  activationToken: string;
  licenseInfo: {
    productName?: string;
    licenseType?: string;
    expiryDate?: string;
    customerName?: string;
    maxUsers?: number;
    maxBranches?: number;
    maxDomains?: number;
    maxDevices?: number;
  } | null;

  // Step 2: Domain
  domainName: string;
  verificationCode: string;

  // Step 3: Hardware
  deviceFingerprint: string;
  deviceName: string;

  // Step 4: Database
  database: {
    host: string;
    port: number;
    database: string;
    username: string;
    password: string;
    sslMode: string;
  };

  // Step 5: Admin
  admin: {
    fullName: string;
    email: string;
    password: string;
    phone: string;
  };

  // Step 6: Company
  company: {
    companyName: string;
    registrationNumber: string;
    gstNumber: string;
    email: string;
    phone: string;
    website: string;
    address: string;
    city: string;
    state: string;
    country: string;
    postalCode: string;
  };

  // Step 7: API
  api: {
    smtpHost: string;
    smtpPort: number;
    smtpUser: string;
    smtpPassword: string;
    smsApiKey: string;
    whatsappApiKey: string;
  };
}

interface InstallerState extends InstallerData {
  currentStep: number;
  completedSteps: number[];
  isInstalled: boolean;

  setStep: (step: number) => void;
  markStepComplete: (step: number) => void;
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  updateField: <K extends keyof InstallerData>(key: K, value: any) => void;
  setInstalled: (installed: boolean) => void;
  reset: () => void;
}

const DEFAULT_DATA: InstallerData = {
  licenseKey: "",
  activationToken: "",
  licenseInfo: null,
  domainName: "",
  verificationCode: "",
  deviceFingerprint: "",
  deviceName: "",
  database: {
    host: "localhost",
    port: 5432,
    database: "license_manager",
    username: "postgres",
    password: "",
    sslMode: "Prefer",
  },
  admin: { fullName: "", email: "", password: "", phone: "" },
  company: {
    companyName: "",
    registrationNumber: "",
    gstNumber: "",
    email: "",
    phone: "",
    website: "",
    address: "",
    city: "",
    state: "",
    country: "",
    postalCode: "",
  },
  api: {
    smtpHost: "",
    smtpPort: 587,
    smtpUser: "",
    smtpPassword: "",
    smsApiKey: "",
    whatsappApiKey: "",
  },
};

export const useInstallerStore = create<InstallerState>()(
  persist(
    (set) => ({
      ...DEFAULT_DATA,
      currentStep: 1,
      completedSteps: [],
      isInstalled: false,

      setStep: (step) => set({ currentStep: step }),
      markStepComplete: (step) =>
        set((state) => ({
          completedSteps: state.completedSteps.includes(step)
            ? state.completedSteps
            : [...state.completedSteps, step],
        })),
      updateField: (key, value) => set({ [key]: value } as never),
      setInstalled: (installed) => set({ isInstalled: installed }),
      reset: () =>
        set({ ...DEFAULT_DATA, currentStep: 1, completedSteps: [], isInstalled: false }),
    }),
    { name: "lm-installer" }
  )
);
