export interface HealthStatus {
  status: string;
  totalDuration: string;
  entries?: Record<
    string,
    {
      status: string;
      duration: string;
      tags?: string[];
    }
  >;
}
