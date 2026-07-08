"use client";

import Link from "next/link";
import { type FormEvent, useCallback, useEffect, useMemo, useState } from "react";
import { routes } from "@/constants/routes";
import { getStoredSession } from "@/features/auth/services/auth-storage";
import type { AuthUser } from "@/features/auth/types/auth.types";
import { ADMIN_EMAIL, isAdminAccount } from "../services/admin-access";
import {
  activateAdminUser,
  createAdminUser,
  deactivateAdminUser,
  deleteAdminUser,
  getAdminRoles,
  getAdminUsers,
  updateAdminUser,
  type AdminRoleItem,
  type AdminUserItem,
  type AdminUserPayload,
} from "../services/admin-data.service";

type StatusFilter = "all" | "active" | "inactive";
type FormMode = "create" | "edit";

type UserFormState = {
  id?: string;
  fullName: string;
  email: string;
  password: string;
  phone: string;
  documentNumber: string;
  roleCodes: string[];
};

type PaginationState = {
  pageNumber: number;
  totalPages: number;
  totalCount: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
};

const pageSize = 10;
const initialPagination: PaginationState = {
  pageNumber: 1,
  totalPages: 1,
  totalCount: 0,
  hasPreviousPage: false,
  hasNextPage: false,
};

function createEmptyForm(defaultRoles: string[] = ["CLIENT"]): UserFormState {
  return {
    fullName: "",
    email: "",
    password: "",
    phone: "",
    documentNumber: "",
    roleCodes: defaultRoles,
  };
}

function getErrorMessage(error: unknown, fallback: string): string {
  return error instanceof Error ? error.message : fallback;
}

function isProtectedAdmin(target: AdminUserItem, sessionUser: AuthUser | null): boolean {
  return target.id === sessionUser?.id || target.email.trim().toLowerCase() === ADMIN_EMAIL;
}

function toPayload(form: UserFormState, mode: FormMode): AdminUserPayload {
  return {
    fullName: form.fullName.trim(),
    email: form.email.trim().toLowerCase(),
    password: mode === "create" ? form.password : undefined,
    phone: form.phone.trim() || null,
    documentNumber: form.documentNumber.trim() || null,
    roleCodes: form.roleCodes,
  };
}

function validateForm(form: UserFormState, mode: FormMode): string | null {
  const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

  if (!form.fullName.trim()) return "El nombre es obligatorio.";
  if (!emailPattern.test(form.email.trim())) return "Ingresa un correo valido.";
  if (mode === "create" && form.password.length < 8) {
    return "La contrasena debe tener al menos 8 caracteres.";
  }
  if (form.roleCodes.length === 0) return "Selecciona al menos un rol.";
  if (form.roleCodes.includes("ADMIN")) {
    return "El rol ADMIN esta reservado para admin@gmail.com.";
  }

  return null;
}

function formatDate(value?: string | null): string {
  if (!value) return "Sin cambios";

  return new Intl.DateTimeFormat("es-CO", {
    day: "2-digit",
    month: "short",
    year: "numeric",
  }).format(new Date(value));
}

export function AdminUsersPage() {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [users, setUsers] = useState<AdminUserItem[]>([]);
  const [roles, setRoles] = useState<AdminRoleItem[]>([]);
  const [pagination, setPagination] = useState<PaginationState>(initialPagination);
  const [isReady, setIsReady] = useState(false);
  const [isLoadingUsers, setIsLoadingUsers] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [actionUserId, setActionUserId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [searchDraft, setSearchDraft] = useState("");
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("all");
  const [formMode, setFormMode] = useState<FormMode>("create");
  const [form, setForm] = useState<UserFormState>(() => createEmptyForm());

  const isAdmin = isAdminAccount(user);
  const availableRoles = useMemo(
    () => roles.filter((role) => role.isActive && role.code !== "ADMIN"),
    [roles],
  );
  const defaultRoleCodes = useMemo(
    () => (availableRoles.some((role) => role.code === "CLIENT") ? ["CLIENT"] : availableRoles.slice(0, 1).map((role) => role.code)),
    [availableRoles],
  );

  const loadUsers = useCallback(
    async (pageNumber = pagination.pageNumber) => {
      setIsLoadingUsers(true);
      setError(null);

      try {
        const response = await getAdminUsers({
          search,
          isActive: statusFilter === "all" ? null : statusFilter === "active",
          pageNumber,
          pageSize,
        });

        setUsers(response.items);
        setPagination({
          pageNumber: response.pageNumber,
          totalPages: response.totalPages || 1,
          totalCount: response.totalCount,
          hasPreviousPage: response.hasPreviousPage,
          hasNextPage: response.hasNextPage,
        });
      } catch (loadError) {
        setError(getErrorMessage(loadError, "No se pudieron cargar los usuarios."));
      } finally {
        setIsLoadingUsers(false);
      }
    },
    [pagination.pageNumber, search, statusFilter],
  );

  const loadRoles = useCallback(async () => {
    try {
      setRoles(await getAdminRoles());
    } catch (loadError) {
      setError(getErrorMessage(loadError, "No se pudieron cargar los roles."));
    }
  }, []);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      const sessionUser = getStoredSession()?.user ?? null;
      setUser(sessionUser);
      setIsReady(true);
    }, 0);

    return () => window.clearTimeout(timeoutId);
  }, []);

  useEffect(() => {
    if (!isReady || !isAdmin) return undefined;

    const timeoutId = window.setTimeout(() => {
      void loadRoles();
    }, 0);

    return () => window.clearTimeout(timeoutId);
  }, [isReady, isAdmin, loadRoles]);

  useEffect(() => {
    if (!isReady || !isAdmin) return undefined;

    const timeoutId = window.setTimeout(() => {
      void loadUsers(pagination.pageNumber);
    }, 0);

    return () => window.clearTimeout(timeoutId);
  }, [isReady, isAdmin, loadUsers, pagination.pageNumber]);

  useEffect(() => {
    if (formMode !== "create") return undefined;

    const timeoutId = window.setTimeout(() => {
      setForm((current) => {
        if (current.roleCodes.length > 0 || defaultRoleCodes.length === 0) return current;
        return { ...current, roleCodes: defaultRoleCodes };
      });
    }, 0);

    return () => window.clearTimeout(timeoutId);
  }, [defaultRoleCodes, formMode]);

  const resetForm = useCallback(() => {
    setFormMode("create");
    setForm(createEmptyForm(defaultRoleCodes.length > 0 ? defaultRoleCodes : ["CLIENT"]));
  }, [defaultRoleCodes]);

  const handleSearchSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setPagination((current) => ({ ...current, pageNumber: 1 }));
    setSearch(searchDraft.trim());
  };

  const handleStatusFilterChange = (nextFilter: StatusFilter) => {
    setPagination((current) => ({ ...current, pageNumber: 1 }));
    setStatusFilter(nextFilter);
  };

  const handleRoleToggle = (roleCode: string) => {
    setForm((current) => {
      const hasRole = current.roleCodes.includes(roleCode);
      return {
        ...current,
        roleCodes: hasRole
          ? current.roleCodes.filter((code) => code !== roleCode)
          : [...current.roleCodes, roleCode],
      };
    });
  };

  const handleEdit = (target: AdminUserItem) => {
    if (isProtectedAdmin(target, user)) return;

    setFormMode("edit");
    setNotice(null);
    setError(null);
    setForm({
      id: target.id,
      fullName: target.fullName,
      email: target.email,
      password: "",
      phone: target.phone ?? "",
      documentNumber: target.documentNumber ?? "",
      roleCodes: target.roles.filter((role) => role !== "ADMIN"),
    });
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const validationError = validateForm(form, formMode);
    if (validationError) {
      setError(validationError);
      return;
    }

    setIsSaving(true);
    setError(null);
    setNotice(null);

    try {
      if (formMode === "edit" && form.id) {
        await updateAdminUser(form.id, toPayload(form, formMode));
        setNotice("Usuario actualizado en base de datos.");
      } else {
        await createAdminUser(toPayload(form, formMode));
        setNotice("Usuario creado en base de datos.");
      }

      resetForm();
      await loadUsers(formMode === "create" ? 1 : pagination.pageNumber);
    } catch (saveError) {
      setError(getErrorMessage(saveError, "No se pudo guardar el usuario."));
    } finally {
      setIsSaving(false);
    }
  };

  const handleToggleActive = async (target: AdminUserItem) => {
    if (isProtectedAdmin(target, user)) return;

    setActionUserId(target.id);
    setError(null);
    setNotice(null);

    try {
      if (target.isActive) {
        await deactivateAdminUser(target.id);
        setNotice("Cuenta desactivada en base de datos.");
      } else {
        await activateAdminUser(target.id);
        setNotice("Cuenta activada en base de datos.");
      }

      await loadUsers(pagination.pageNumber);
    } catch (actionError) {
      setError(getErrorMessage(actionError, "No se pudo cambiar el estado de la cuenta."));
    } finally {
      setActionUserId(null);
    }
  };

  const handleDelete = async (target: AdminUserItem) => {
    if (isProtectedAdmin(target, user)) return;

    const shouldDelete = window.confirm(`Eliminar la cuenta de ${target.email}?`);
    if (!shouldDelete) return;

    setActionUserId(target.id);
    setError(null);
    setNotice(null);

    try {
      await deleteAdminUser(target.id);
      setNotice("Usuario eliminado en base de datos.");
      resetForm();
      await loadUsers(pagination.pageNumber);
    } catch (deleteError) {
      setError(getErrorMessage(deleteError, "No se pudo eliminar el usuario."));
    } finally {
      setActionUserId(null);
    }
  };

  if (!isReady) {
    return (
      <main className="min-h-screen bg-background px-5 pb-16 pt-36 text-foreground sm:px-8 md:pt-24 lg:px-12">
        <section className="mx-auto max-w-6xl border-y border-border/70 bg-paper/55 py-14 text-center">
          <p className="text-sm font-black uppercase tracking-widest text-muted">
            Cargando usuarios
          </p>
        </section>
      </main>
    );
  }

  if (!isAdmin) {
    return (
      <main className="min-h-screen bg-background px-5 pb-16 pt-36 text-foreground sm:px-8 md:pt-24 lg:px-12">
        <section className="mx-auto max-w-3xl border-y border-border/70 bg-paper/55 px-5 py-16 text-center">
          <h1 className="text-3xl font-black text-foreground">
            Acceso administrativo
          </h1>
          <p className="mx-auto mt-3 max-w-md text-sm font-semibold leading-6 text-muted">
            Usuarios esta disponible solo para la cuenta administradora.
          </p>
          <Link
            href="/auth/login"
            className="mt-7 inline-flex h-11 items-center justify-center rounded-full bg-foreground px-6 text-sm font-black text-paper transition hover:bg-accent focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
          >
            Iniciar sesion
          </Link>
        </section>
      </main>
    );
  }

  return (
    <main className="min-h-screen bg-background px-5 pb-16 pt-36 text-foreground sm:px-8 md:pt-24 lg:px-12">
      <div className="mx-auto max-w-7xl">
        <section className="flex flex-col gap-4 border-b border-border/70 pb-7 sm:flex-row sm:items-end sm:justify-between">
          <div>
            <p className="text-[0.68rem] font-black uppercase tracking-widest text-accent">
              Administrador
            </p>
            <h1 className="mt-2 text-4xl font-black leading-tight text-foreground sm:text-5xl">
              Usuarios
            </h1>
            <p className="mt-3 max-w-2xl text-sm font-semibold leading-6 text-muted">
              {pagination.totalCount} cuentas registradas en Webook.
            </p>
          </div>
          <Link
            href={routes.adminInventory}
            className="inline-flex h-10 items-center justify-center rounded-full border border-[rgba(53,30,28,0.24)] bg-paper px-5 text-[0.68rem] font-black uppercase tracking-widest text-foreground transition hover:border-accent hover:text-accent focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
          >
            Inventario
          </Link>
        </section>

        <section className="mt-7 grid gap-3 lg:grid-cols-[1fr_auto]">
          <form
            onSubmit={handleSearchSubmit}
            className="grid gap-3 rounded-[2rem] border border-[rgba(53,30,28,0.16)] bg-paper/75 p-2 shadow-[0_12px_28px_rgba(53,30,28,0.05)] sm:grid-cols-[1fr_auto]"
          >
            <label className="sr-only" htmlFor="admin-user-search">
              Buscar usuario
            </label>
            <input
              id="admin-user-search"
              value={searchDraft}
              onChange={(event) => setSearchDraft(event.target.value)}
              placeholder="Nombre, correo o ID"
              className="h-11 rounded-full border border-transparent bg-transparent px-4 text-sm font-bold text-foreground outline-none placeholder:text-muted focus:border-[rgba(53,30,28,0.24)] focus:bg-background"
            />
            <button
              type="submit"
              className="h-11 rounded-full bg-foreground px-6 text-[0.68rem] font-black uppercase tracking-widest text-paper shadow-[0_10px_22px_rgba(53,30,28,0.16)] transition hover:bg-accent focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
            >
              Buscar
            </button>
          </form>

          <div className="flex rounded-full border border-[rgba(53,30,28,0.16)] bg-paper/75 p-1 shadow-[0_12px_28px_rgba(53,30,28,0.05)]">
            {(["all", "active", "inactive"] as const).map((filter) => (
              <button
                key={filter}
                type="button"
                onClick={() => handleStatusFilterChange(filter)}
                className={`h-10 rounded-full px-4 text-[0.64rem] font-black uppercase tracking-widest transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent ${
                  statusFilter === filter
                    ? "bg-foreground text-paper"
                    : "text-muted hover:text-foreground"
                }`}
              >
                {filter === "all" ? "Todos" : filter === "active" ? "Activos" : "Inactivos"}
              </button>
            ))}
          </div>
        </section>

        {error ? (
          <div className="mt-6 border border-accent/25 bg-accent/5 px-5 py-4 text-sm font-bold text-foreground">
            {error}
          </div>
        ) : null}

        {notice ? (
          <div className="mt-6 border border-[#b8d8c0] bg-[#eef8f0] px-5 py-4 text-sm font-bold text-[#315f3a]">
            {notice}
          </div>
        ) : null}

        <section className="mt-8 grid gap-6 lg:grid-cols-[minmax(0,1fr)_360px]">
          <div className="overflow-hidden border border-[rgba(53,30,28,0.14)] bg-paper/65">
            <div className="grid grid-cols-[1fr_auto] gap-3 border-b border-border/70 px-4 py-3 text-[0.62rem] font-black uppercase tracking-widest text-muted sm:grid-cols-[1fr_150px_160px_auto]">
              <span>Cuenta</span>
              <span className="hidden sm:block">Estado</span>
              <span className="hidden sm:block">Actualizado</span>
              <span>Acciones</span>
            </div>

            {isLoadingUsers ? (
              <div className="px-4 py-12 text-center text-sm font-black uppercase tracking-widest text-muted">
                Cargando
              </div>
            ) : users.length === 0 ? (
              <div className="px-4 py-12 text-center text-sm font-bold text-muted">
                No hay usuarios para este filtro.
              </div>
            ) : (
              <div className="divide-y divide-border/70">
                {users.map((item) => {
                  const protectedAdmin = isProtectedAdmin(item, user);
                  const isBusy = actionUserId === item.id;

                  return (
                    <article
                      key={item.id}
                      className="grid gap-4 px-4 py-4 sm:grid-cols-[1fr_150px_160px_auto] sm:items-center"
                    >
                      <div className="min-w-0">
                        <div className="flex flex-wrap items-center gap-2">
                          <h2 className="truncate text-sm font-black text-foreground">
                            {item.fullName}
                          </h2>
                          {protectedAdmin ? (
                            <span className="rounded-full border border-accent/25 bg-accent/10 px-2.5 py-1 text-[0.58rem] font-black uppercase tracking-widest text-accent">
                              Admin
                            </span>
                          ) : null}
                        </div>
                        <p className="mt-1 truncate text-xs font-bold text-muted">
                          {item.email}
                        </p>
                        <p className="mt-1 truncate text-[0.68rem] font-bold text-muted/80">
                          ID {item.id}
                        </p>
                        <div className="mt-3 flex flex-wrap gap-2">
                          {item.roles.length > 0 ? item.roles.map((role) => (
                            <span
                              key={role}
                              className="rounded-full border border-[rgba(53,30,28,0.16)] bg-[#f8efe9] px-3 py-1 text-[0.58rem] font-black uppercase tracking-widest text-foreground"
                            >
                              {role}
                            </span>
                          )) : (
                            <span className="text-xs font-bold text-muted">
                              Sin rol
                            </span>
                          )}
                        </div>
                      </div>

                      <span
                        className={`w-fit rounded-full border px-3 py-1 text-[0.62rem] font-black uppercase tracking-widest ${
                          item.isActive
                            ? "border-[#b8d8c0] bg-[#eef8f0] text-[#315f3a]"
                            : "border-accent/30 bg-accent/10 text-accent"
                        }`}
                      >
                        {item.isActive ? "Activo" : "Inactivo"}
                      </span>

                      <span className="text-xs font-bold text-muted">
                        {formatDate(item.updatedAt ?? item.createdAt)}
                      </span>

                      <div className="flex flex-wrap gap-2 sm:justify-end">
                        <button
                          type="button"
                          onClick={() => handleEdit(item)}
                          disabled={protectedAdmin || isBusy}
                          className="h-9 rounded-full border border-[rgba(53,30,28,0.2)] bg-paper px-4 text-[0.62rem] font-black uppercase tracking-widest text-foreground transition hover:border-accent hover:text-accent disabled:cursor-not-allowed disabled:opacity-45"
                        >
                          Editar
                        </button>
                        <button
                          type="button"
                          onClick={() => handleToggleActive(item)}
                          disabled={protectedAdmin || isBusy}
                          className="h-9 rounded-full border border-[rgba(53,30,28,0.2)] bg-paper px-4 text-[0.62rem] font-black uppercase tracking-widest text-foreground transition hover:border-accent hover:text-accent disabled:cursor-not-allowed disabled:opacity-45"
                        >
                          {item.isActive ? "Desactivar" : "Activar"}
                        </button>
                        <button
                          type="button"
                          onClick={() => handleDelete(item)}
                          disabled={protectedAdmin || item.isActive || isBusy}
                          title={item.isActive ? "Desactiva la cuenta antes de eliminarla" : undefined}
                          className="h-9 rounded-full bg-foreground px-4 text-[0.62rem] font-black uppercase tracking-widest text-paper transition hover:bg-accent disabled:cursor-not-allowed disabled:opacity-45"
                        >
                          Eliminar
                        </button>
                      </div>
                    </article>
                  );
                })}
              </div>
            )}
          </div>

          <form
            onSubmit={handleSubmit}
            className="h-fit border border-[rgba(53,30,28,0.16)] bg-paper/75 p-5 shadow-[0_14px_34px_rgba(53,30,28,0.07)]"
          >
            <div className="flex items-start justify-between gap-3">
              <div>
                <p className="text-[0.62rem] font-black uppercase tracking-widest text-accent">
                  {formMode === "create" ? "Nueva cuenta" : "Editar cuenta"}
                </p>
                <h2 className="mt-2 text-xl font-black text-foreground">
                  {formMode === "create" ? "Crear usuario" : form.email}
                </h2>
              </div>
              {formMode === "edit" ? (
                <button
                  type="button"
                  onClick={resetForm}
                  className="h-9 rounded-full border border-[rgba(53,30,28,0.18)] bg-background px-4 text-[0.62rem] font-black uppercase tracking-widest text-foreground transition hover:border-accent hover:text-accent"
                >
                  Cancelar
                </button>
              ) : null}
            </div>

            <div className="mt-5 grid gap-3">
              <label className="grid gap-1 text-xs font-black uppercase tracking-widest text-muted">
                Nombre
                <input
                  value={form.fullName}
                  onChange={(event) => setForm((current) => ({ ...current, fullName: event.target.value }))}
                  className="h-11 rounded-full border border-[rgba(53,30,28,0.2)] bg-background px-4 text-sm font-bold normal-case tracking-normal text-foreground outline-none focus:border-accent"
                />
              </label>

              <label className="grid gap-1 text-xs font-black uppercase tracking-widest text-muted">
                Correo
                <input
                  type="email"
                  value={form.email}
                  onChange={(event) => setForm((current) => ({ ...current, email: event.target.value }))}
                  className="h-11 rounded-full border border-[rgba(53,30,28,0.2)] bg-background px-4 text-sm font-bold normal-case tracking-normal text-foreground outline-none focus:border-accent"
                />
              </label>

              {formMode === "create" ? (
                <label className="grid gap-1 text-xs font-black uppercase tracking-widest text-muted">
                  Contrasena
                  <input
                    type="password"
                    value={form.password}
                    onChange={(event) => setForm((current) => ({ ...current, password: event.target.value }))}
                    className="h-11 rounded-full border border-[rgba(53,30,28,0.2)] bg-background px-4 text-sm font-bold normal-case tracking-normal text-foreground outline-none focus:border-accent"
                  />
                </label>
              ) : null}

              <label className="grid gap-1 text-xs font-black uppercase tracking-widest text-muted">
                Telefono
                <input
                  value={form.phone}
                  onChange={(event) => setForm((current) => ({ ...current, phone: event.target.value }))}
                  className="h-11 rounded-full border border-[rgba(53,30,28,0.2)] bg-background px-4 text-sm font-bold normal-case tracking-normal text-foreground outline-none focus:border-accent"
                />
              </label>

              <label className="grid gap-1 text-xs font-black uppercase tracking-widest text-muted">
                Documento
                <input
                  value={form.documentNumber}
                  onChange={(event) => setForm((current) => ({ ...current, documentNumber: event.target.value }))}
                  className="h-11 rounded-full border border-[rgba(53,30,28,0.2)] bg-background px-4 text-sm font-bold normal-case tracking-normal text-foreground outline-none focus:border-accent"
                />
              </label>

              <fieldset className="grid gap-2">
                <legend className="text-xs font-black uppercase tracking-widest text-muted">
                  Roles
                </legend>
                <div className="flex flex-wrap gap-2">
                  {availableRoles.map((role) => {
                    const isSelected = form.roleCodes.includes(role.code);

                    return (
                      <label
                        key={role.code}
                        className={`cursor-pointer rounded-full border px-4 py-2 text-[0.62rem] font-black uppercase tracking-widest transition ${
                          isSelected
                            ? "border-foreground bg-foreground text-paper"
                            : "border-[rgba(53,30,28,0.18)] bg-background text-muted hover:text-foreground"
                        }`}
                      >
                        <input
                          type="checkbox"
                          checked={isSelected}
                          onChange={() => handleRoleToggle(role.code)}
                          className="sr-only"
                        />
                        {role.name || role.code}
                      </label>
                    );
                  })}
                </div>
              </fieldset>
            </div>

            <button
              type="submit"
              disabled={isSaving || availableRoles.length === 0}
              className="mt-6 h-11 w-full rounded-full bg-foreground px-5 text-[0.68rem] font-black uppercase tracking-widest text-paper shadow-[0_12px_26px_rgba(53,30,28,0.18)] transition hover:bg-accent focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent disabled:cursor-not-allowed disabled:opacity-55"
            >
              {isSaving ? "Guardando" : formMode === "create" ? "Crear" : "Guardar"}
            </button>
          </form>
        </section>

        <section className="mt-6 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <p className="text-xs font-bold text-muted">
            Pagina {pagination.pageNumber} de {pagination.totalPages}
          </p>
          <div className="flex gap-2">
            <button
              type="button"
              disabled={!pagination.hasPreviousPage || isLoadingUsers}
              onClick={() => setPagination((current) => ({ ...current, pageNumber: Math.max(1, current.pageNumber - 1) }))}
              className="h-10 rounded-full border border-[rgba(53,30,28,0.2)] bg-paper px-5 text-[0.62rem] font-black uppercase tracking-widest text-foreground transition hover:border-accent hover:text-accent disabled:cursor-not-allowed disabled:opacity-45"
            >
              Anterior
            </button>
            <button
              type="button"
              disabled={!pagination.hasNextPage || isLoadingUsers}
              onClick={() => setPagination((current) => ({ ...current, pageNumber: current.pageNumber + 1 }))}
              className="h-10 rounded-full border border-[rgba(53,30,28,0.2)] bg-paper px-5 text-[0.62rem] font-black uppercase tracking-widest text-foreground transition hover:border-accent hover:text-accent disabled:cursor-not-allowed disabled:opacity-45"
            >
              Siguiente
            </button>
          </div>
        </section>
      </div>
    </main>
  );
}

