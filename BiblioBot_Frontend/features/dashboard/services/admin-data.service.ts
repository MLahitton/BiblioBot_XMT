import { getStoredSession } from "@/features/auth/services/auth-storage";
import { API_ENDPOINTS } from "@/lib/api/endpoints";
import { apiDelete, apiGet, apiPatch, apiPost, apiPut } from "@/lib/api/api-client";

type PagedResponse<TItem> = {
  items: TItem[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
};

export type AdminInventoryItem = {
  inventoryStockId: string;
  bookId: string;
  bookTitle: string;
  isbn?: string | null;
  branchId: string;
  branchName: string;
  currentStock: number;
  minStock: number;
  isLowStock: boolean;
  updatedAt?: string | null;
};

export type AdminProductSort =
  | "title"
  | "author"
  | "price_asc"
  | "price_desc"
  | "purchased_desc"
  | "favorites_desc";

export type AdminProductItem = {
  id: string;
  title: string;
  isbn?: string | null;
  description?: string | null;
  publisherName?: string | null;
  publicationYear?: number | null;
  language?: string | null;
  imageUrl?: string | null;
  price: number;
  isActive: boolean;
  authors: string[];
  categories: string[];
  branchId?: string | null;
  branchName?: string | null;
  currentStock: number;
  minStock: number;
  purchasedCount: number;
  favoriteCount: number;
  createdAt: string;
  updatedAt?: string | null;
};

export type AdminProductPayload = {
  title: string;
  isbn?: string | null;
  description?: string | null;
  publisherName?: string | null;
  publicationYear?: number | null;
  language?: string | null;
  imageUrl?: string | null;
  price: number;
  authorNames: string[];
  categoryNames: string[];
  branchId?: string | null;
  currentStock: number;
  minStock: number;
};

export type AdminUserItem = {
  id: string;
  fullName: string;
  email: string;
  phone?: string | null;
  documentNumber?: string | null;
  roles: string[];
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
};

type AdminUserDetailResponse = Omit<AdminUserItem, "roles"> & {
  roles: Array<{ code: string }>;
};

export type AdminRoleItem = {
  id: string;
  code: string;
  name: string;
  description?: string | null;
  isActive: boolean;
  permissionsCount: number;
};

export type AdminUserPayload = {
  fullName: string;
  email: string;
  password?: string;
  phone?: string | null;
  documentNumber?: string | null;
  roleCodes: string[];
};

class AdminDataAuthError extends Error {
  constructor() {
    super("Debes iniciar sesion con la cuenta administradora.");
    this.name = "AdminDataAuthError";
  }
}

function getAccessToken(): string {
  const token = getStoredSession()?.accessToken;

  if (!token) {
    throw new AdminDataAuthError();
  }

  return token;
}

export async function getAdminInventory(): Promise<PagedResponse<AdminInventoryItem>> {
  return apiGet<PagedResponse<AdminInventoryItem>>(API_ENDPOINTS.inventory, {
    token: getAccessToken(),
    query: {
      pageNumber: 1,
      pageSize: 8,
    },
  });
}

export async function getAdminProducts(options: {
  search?: string;
  isActive?: boolean | null;
  sortBy?: AdminProductSort;
  pageNumber?: number;
  pageSize?: number;
} = {}): Promise<PagedResponse<AdminProductItem>> {
  return apiGet<PagedResponse<AdminProductItem>>(API_ENDPOINTS.adminProducts.list, {
    token: getAccessToken(),
    query: {
      search: options.search,
      isActive: options.isActive,
      sortBy: options.sortBy,
      pageNumber: options.pageNumber ?? 1,
      pageSize: options.pageSize ?? 12,
    },
  });
}

export async function getAdminUsers(options: {
  search?: string;
  isActive?: boolean | null;
  pageNumber?: number;
  pageSize?: number;
} = {}): Promise<PagedResponse<AdminUserItem>> {
  return apiGet<PagedResponse<AdminUserItem>>(API_ENDPOINTS.adminUsers.list, {
    token: getAccessToken(),
    query: {
      search: options.search,
      isActive: options.isActive,
      pageNumber: options.pageNumber ?? 1,
      pageSize: options.pageSize ?? 12,
    },
  });
}

export async function getAdminRoles(): Promise<AdminRoleItem[]> {
  return apiGet<AdminRoleItem[]>(API_ENDPOINTS.adminRoles, {
    token: getAccessToken(),
  });
}

function mapUserDetailToItem(user: AdminUserDetailResponse): AdminUserItem {
  return {
    ...user,
    roles: user.roles.map((role) => role.code),
  };
}

export async function createAdminUser(payload: AdminUserPayload): Promise<AdminUserItem> {
  const user = await apiPost<AdminUserDetailResponse, AdminUserPayload>(
    API_ENDPOINTS.adminUsers.list,
    payload,
    { token: getAccessToken() },
  );

  return mapUserDetailToItem(user);
}

export async function updateAdminUser(
  id: string,
  payload: AdminUserPayload,
): Promise<AdminUserItem> {
  const { password: _password, ...updatePayload } = payload;
  void _password;

  const user = await apiPut<AdminUserDetailResponse, Omit<AdminUserPayload, "password">>(
    API_ENDPOINTS.adminUsers.item(id),
    updatePayload,
    { token: getAccessToken() },
  );

  return mapUserDetailToItem(user);
}

export async function activateAdminUser(id: string): Promise<AdminUserItem> {
  const user = await apiPatch<AdminUserDetailResponse>(
    API_ENDPOINTS.adminUsers.activate(id),
    undefined,
    { token: getAccessToken() },
  );

  return mapUserDetailToItem(user);
}

export async function deactivateAdminUser(id: string): Promise<AdminUserItem> {
  const user = await apiPatch<AdminUserDetailResponse>(
    API_ENDPOINTS.adminUsers.deactivate(id),
    undefined,
    { token: getAccessToken() },
  );

  return mapUserDetailToItem(user);
}

export async function deleteAdminUser(id: string): Promise<void> {
  await apiDelete<void>(API_ENDPOINTS.adminUsers.item(id), {
    token: getAccessToken(),
  });
}

export async function createAdminProduct(
  payload: AdminProductPayload,
): Promise<AdminProductItem> {
  return apiPost<AdminProductItem, AdminProductPayload>(
    API_ENDPOINTS.adminProducts.list,
    payload,
    { token: getAccessToken() },
  );
}

export async function updateAdminProduct(
  id: string,
  payload: AdminProductPayload,
): Promise<AdminProductItem> {
  return apiPut<AdminProductItem, AdminProductPayload>(
    API_ENDPOINTS.adminProducts.item(id),
    payload,
    { token: getAccessToken() },
  );
}

export async function activateAdminProduct(id: string): Promise<void> {
  await apiPatch<void>(API_ENDPOINTS.adminProducts.activate(id), undefined, {
    token: getAccessToken(),
  });
}

export async function deactivateAdminProduct(id: string): Promise<void> {
  await apiPatch<void>(API_ENDPOINTS.adminProducts.deactivate(id), undefined, {
    token: getAccessToken(),
  });
}

export async function deleteAdminProduct(id: string): Promise<void> {
  await apiDelete<void>(API_ENDPOINTS.adminProducts.item(id), {
    token: getAccessToken(),
  });
}
