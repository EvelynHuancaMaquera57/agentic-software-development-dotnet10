import { request } from "./client";
import type { DashboardDays, DashboardResponse } from "./types";

export const DASHBOARD_PERIODS: readonly DashboardDays[] = [7, 30, 90];
export const DEFAULT_DASHBOARD_DAYS: DashboardDays = 30;

export function isDashboardDays(value: number): value is DashboardDays {
  return (DASHBOARD_PERIODS as readonly number[]).includes(value);
}

/** GET /api/dashboard?days= → totales, periodo vs anterior, serie diaria y top URLs. */
export function getDashboard(days: DashboardDays, signal?: AbortSignal): Promise<DashboardResponse> {
  return request<DashboardResponse>("GET", `/api/dashboard?days=${days}`, signal ? { signal } : {});
}
