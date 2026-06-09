import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  output: "standalone",
  env: {
    // Expose NEXT_PUBLIC_API_URL at build-time when set in the environment
    NEXT_PUBLIC_API_URL: process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000",
  },
};

export default nextConfig;
