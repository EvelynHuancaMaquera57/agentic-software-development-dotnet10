import { request } from "./client";
import type { CreateUrlRequest, MyUrl, PagedResponse, UrlResponse } from "./types";

export const MY_URLS_PAGE_SIZE = 10;

/** POST /api/urls → 201. Errores: 400 (campos url / alias), 409 (alias en uso), 401. */
export function createUrl(body: CreateUrlRequest): Promise<UrlResponse> {
  return request<UrlResponse>("POST", "/api/urls", { body });
}

/** GET /api/urls/mine → las URLs del usuario, más recientes primero, con sus clicks. */
export function listMyUrls(page: number, signal?: AbortSignal): Promise<PagedResponse<MyUrl>> {
  const query = new URLSearchParams({ page: String(page), pageSize: String(MY_URLS_PAGE_SIZE) });
  return request<PagedResponse<MyUrl>>("GET", `/api/urls/mine?${query}`, signal ? { signal } : {});
}
