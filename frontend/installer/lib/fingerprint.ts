/**
 * Generate a device fingerprint based on browser/system characteristics
 * for hardware verification during installation.
 */
export interface DeviceInfo {
  userAgent: string;
  platform: string;
  language: string;
  screenResolution: string;
  colorDepth: string;
  timezone: string;
  hardwareConcurrency: string;
  deviceMemory: string;
}

export function collectDeviceInfo(): DeviceInfo {
  if (typeof window === "undefined") {
    return {
      userAgent: "",
      platform: "",
      language: "",
      screenResolution: "",
      colorDepth: "",
      timezone: "",
      hardwareConcurrency: "",
      deviceMemory: "",
    };
  }

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const nav = navigator as any;

  return {
    userAgent: navigator.userAgent,
    platform: navigator.platform || "unknown",
    language: navigator.language,
    screenResolution: `${screen.width}x${screen.height}`,
    colorDepth: String(screen.colorDepth),
    timezone: Intl.DateTimeFormat().resolvedOptions().timeZone,
    hardwareConcurrency: String(navigator.hardwareConcurrency || 0),
    deviceMemory: String(nav.deviceMemory || 0),
  };
}

export async function generateFingerprint(info: DeviceInfo): Promise<string> {
  const sorted = Object.entries(info)
    .sort(([a], [b]) => a.localeCompare(b))
    .map(([k, v]) => `${k}:${v}`)
    .join(";");

  const encoder = new TextEncoder();
  const data = encoder.encode(sorted);
  const hashBuffer = await crypto.subtle.digest("SHA-256", data);
  const hashArray = Array.from(new Uint8Array(hashBuffer));
  return hashArray.map((b) => b.toString(16).padStart(2, "0")).join("").toUpperCase();
}
