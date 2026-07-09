export type AuthUser = {
  id: string;
  fullName: string;
  email: string;
  phone?: string | null;
  documentNumber?: string | null;
  roles: string[];
  permissions: string[];
};

export type AuthSession = {
  accessToken: string;
  refreshToken: string;
  user: AuthUser;
};

export type LoginPayload = {
  email: string;
  password: string;
};

export type RegisterPayload = {
  fullName: string;
  email: string;
  password: string;
  phone?: string | null;
  documentNumber?: string | null;
};
