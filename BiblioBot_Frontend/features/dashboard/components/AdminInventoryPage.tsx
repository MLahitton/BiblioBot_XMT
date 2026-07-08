"use client";

import Image from "next/image";
import Link from "next/link";
import { type FormEvent, useCallback, useEffect, useMemo, useState } from "react";
import { defaultPriceLocale, priceFormatOptions } from "@/constants/currency";
import { routes } from "@/constants/routes";
import { getStoredSession } from "@/features/auth/services/auth-storage";
import type { AuthUser } from "@/features/auth/types/auth.types";
import { isAdminAccount } from "../services/admin-access";
import {
  activateAdminProduct,
  createAdminProduct,
  deactivateAdminProduct,
  deleteAdminProduct,
  getAdminProducts,
  updateAdminProduct,
  type AdminProductItem,
  type AdminProductPayload,
  type AdminProductSort,
} from "../services/admin-data.service";

type StatusFilter = "all" | "active" | "inactive";
type FormMode = "create" | "edit";

type ProductFormState = {
  id?: string;
  title: string;
  isbn: string;
  description: string;
  publisherName: string;
  publicationYear: string;
  language: string;
  imageUrl: string;
  price: string;
  authorNames: string;
  mainCategory: string;
  subcategory: string;
  currentStock: string;
  minStock: string;
};

type PaginationState = {
  pageNumber: number;
  totalPages: number;
  totalCount: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
};

const pageSize = 8;
const fallbackCover = "/images/books/book-01.svg";
const imageFormatText = "Portada vertical 2:3 en JPG, PNG, WebP o SVG.";
const priceFormatter = new Intl.NumberFormat(defaultPriceLocale, priceFormatOptions);
const subtleScrollbarClassName = "[scrollbar-color:rgba(53,30,28,0.2)_transparent] [scrollbar-width:thin] [&::-webkit-scrollbar]:w-1.5 [&::-webkit-scrollbar-track]:bg-transparent [&::-webkit-scrollbar-thumb]:rounded-full [&::-webkit-scrollbar-thumb]:bg-[rgba(53,30,28,0.2)] [&::-webkit-scrollbar-thumb:hover]:bg-[rgba(53,30,28,0.34)]";
const dropdownListboxClassName = `absolute left-0 top-[calc(100%+8px)] z-40 max-h-72 w-full overscroll-contain overflow-y-auto border border-[rgba(53,30,28,0.16)] bg-paper p-1.5 shadow-[0_18px_42px_rgba(53,30,28,0.16)] ${subtleScrollbarClassName}`;
const textareaClassName = `w-full min-w-0 resize-none rounded-[18px] border border-[rgba(53,30,28,0.2)] bg-background py-3 pl-4 pr-6 text-sm font-bold normal-case tracking-normal text-foreground outline-none transition focus:border-accent ${subtleScrollbarClassName}`;

const initialPagination: PaginationState = {
  pageNumber: 1,
  totalPages: 1,
  totalCount: 0,
  hasPreviousPage: false,
  hasNextPage: false,
};

const sortOptions: Array<{
  value: AdminProductSort;
  label: string;
  detail: string;
}> = [
  { value: "title", label: "Nombre A-Z", detail: "Orden alfabético" },
  { value: "author", label: "Autor A-Z", detail: "Por autor principal" },
  { value: "price_asc", label: "Menor precio", detail: "Económicos primero" },
  { value: "price_desc", label: "Mayor precio", detail: "Precio alto primero" },
  { value: "purchased_desc", label: "Más comprados", detail: "Mayor movimiento" },
  { value: "favorites_desc", label: "Más guardados", detail: "Más favoritos" },
];

const categoryGroups = [
  { name: "Ficción", subcategories: ["Fantasía épica", "Fantasía urbana", "Ciencia ficción", "Distopía", "Romance", "Romántica juvenil", "Terror", "Misterio", "Thriller", "Novela negra", "Histórica", "Aventura", "Realismo mágico", "Contemporánea", "Dark romance"] },
  { name: "No ficción", subcategories: ["Biografías", "Autobiografías", "Historia", "Filosofía", "Ensayo", "Autoayuda", "Psicología", "Finanzas personales", "Emprendimiento", "Marketing", "Productividad", "Ciencia", "Tecnología", "Programación", "Liderazgo"] },
  { name: "Infantil", subcategories: ["Primeras lecturas", "Cuentos ilustrados", "Aprendizaje temprano", "Actividades", "Colorear"] },
  { name: "Juvenil", subcategories: ["Juvenil fantasy", "Juvenil romance", "Juvenil aventura", "Romántica juvenil"] },
  { name: "Académicos", subcategories: ["Matemáticas", "Física", "Química", "Biología", "Derecho", "Medicina", "Economía", "Administración", "Ingeniería", "Informática", "Lenguas"] },
  { name: "Cómic y novela gráfica", subcategories: [] },
  { name: "Poesía", subcategories: [] },
  { name: "Teatro", subcategories: [] },
  { name: "Audiolibros", subcategories: [] },
  { name: "Religión", subcategories: [] },
  { name: "Arte", subcategories: [] },
  { name: "Cocina", subcategories: [] },
  { name: "Viajes", subcategories: [] },
  { name: "Salud", subcategories: [] },
  { name: "Tecnología", subcategories: [] },
  { name: "Negocios", subcategories: [] },
  { name: "Idiomas", subcategories: [] },
  { name: "Bolsillo", subcategories: [] },
  { name: "Coleccionables", subcategories: [] },
  { name: "Ediciones especiales", subcategories: [] },
] as const;

const mainCategoryNames: string[] = categoryGroups.map((group) => group.name);

function createEmptyForm(): ProductFormState {
  return {
    title: "",
    isbn: "",
    description: "",
    publisherName: "",
    publicationYear: "",
    language: "es",
    imageUrl: "",
    price: "",
    authorNames: "",
    mainCategory: "",
    subcategory: "",
    currentStock: "0",
    minStock: "0",
  };
}

function getErrorMessage(error: unknown, fallback: string): string {
  return error instanceof Error ? error.message : fallback;
}

function formatPrice(value: number): string {
  return priceFormatter.format(value);
}

function splitNames(value: string): string[] {
  return value
    .split(",")
    .map((item) => item.trim())
    .filter(Boolean);
}

function getSubcategories(mainCategory: string): readonly string[] {
  return categoryGroups.find((group) => group.name === mainCategory)?.subcategories ?? [];
}

function getPayloadCategories(form: ProductFormState): string[] {
  return [form.mainCategory, form.subcategory]
    .map((category) => category.trim())
    .filter(Boolean);
}

function findMainCategory(categories: readonly string[]): string {
  const categorySet = new Set(categories);
  return mainCategoryNames.find((category) => categorySet.has(category)) ?? "";
}

function findSubcategory(categories: readonly string[], mainCategory: string): string {
  const subcategories = getSubcategories(mainCategory);
  return categories.find((category) => subcategories.includes(category)) ?? "";
}

function parseInteger(value: string): number {
  const parsed = Number.parseInt(value, 10);
  return Number.isNaN(parsed) ? 0 : parsed;
}

function parseDecimal(value: string): number {
  const normalized = value.replace(",", ".");
  const parsed = Number.parseFloat(normalized);
  return Number.isNaN(parsed) ? 0 : parsed;
}

function isImageSourceValid(value: string): boolean {
  const source = value.trim();
  if (!source) return true;

  const hasValidExtension = /\.(jpg|jpeg|png|webp|svg)(\?.*)?$/i.test(source);
  const isDataImage = /^data:image\/(png|jpe?g|webp|svg\+xml);base64,/i.test(source);

  return (source.startsWith("http://") || source.startsWith("https://") || source.startsWith("/"))
    && hasValidExtension
    || isDataImage;
}

function validateForm(form: ProductFormState): string | null {
  if (!form.title.trim()) return "El nombre del libro es obligatorio.";
  if (!form.mainCategory) return "Selecciona la categoría principal del libro.";
  if (parseDecimal(form.price) < 0) return "El precio debe ser mayor o igual a 0.";
  if (parseInteger(form.currentStock) < 0) return "El stock debe ser mayor o igual a 0.";
  if (parseInteger(form.minStock) < 0) return "El stock mínimo debe ser mayor o igual a 0.";
  if (form.publicationYear.trim() && parseInteger(form.publicationYear) < 1) {
    return "El año de publicación debe ser mayor a 0.";
  }
  if (!isImageSourceValid(form.imageUrl)) {
    return "La imagen debe ser una URL directa JPG, PNG, WebP o SVG con formato vertical 2:3.";
  }

  return null;
}

function toPayload(form: ProductFormState): AdminProductPayload {
  return {
    title: form.title.trim(),
    isbn: form.isbn.trim() || null,
    description: form.description.trim() || null,
    publisherName: form.publisherName.trim() || null,
    publicationYear: form.publicationYear.trim() ? parseInteger(form.publicationYear) : null,
    language: form.language.trim() || null,
    imageUrl: form.imageUrl.trim() || null,
    price: parseDecimal(form.price),
    authorNames: splitNames(form.authorNames),
    categoryNames: getPayloadCategories(form),
    branchId: null,
    currentStock: parseInteger(form.currentStock),
    minStock: parseInteger(form.minStock),
  };
}

function toForm(product: AdminProductItem): ProductFormState {
  const mainCategory = findMainCategory(product.categories);

  return {
    id: product.id,
    title: product.title,
    isbn: product.isbn ?? "",
    description: product.description ?? "",
    publisherName: product.publisherName ?? "",
    publicationYear: product.publicationYear?.toString() ?? "",
    language: product.language ?? "es",
    imageUrl: product.imageUrl ?? "",
    price: product.price.toString(),
    authorNames: product.authors.join(", "),
    mainCategory,
    subcategory: findSubcategory(product.categories, mainCategory),
    currentStock: product.currentStock.toString(),
    minStock: product.minStock.toString(),
  };
}

export function AdminInventoryPage() {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [products, setProducts] = useState<AdminProductItem[]>([]);
  const [pagination, setPagination] = useState<PaginationState>(initialPagination);
  const [isReady, setIsReady] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [actionProductId, setActionProductId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [searchDraft, setSearchDraft] = useState("");
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("all");
  const [sortBy, setSortBy] = useState<AdminProductSort>("title");
  const [isSortOpen, setIsSortOpen] = useState(false);
  const [isMainCategoryOpen, setIsMainCategoryOpen] = useState(false);
  const [isSubcategoryOpen, setIsSubcategoryOpen] = useState(false);
  const [formMode, setFormMode] = useState<FormMode>("create");
  const [form, setForm] = useState<ProductFormState>(() => createEmptyForm());

  const isAdmin = isAdminAccount(user);
  const previewImage = useMemo(
    () => form.imageUrl.trim() || fallbackCover,
    [form.imageUrl],
  );
  const selectedSort = useMemo(
    () => sortOptions.find((option) => option.value === sortBy) ?? sortOptions[0],
    [sortBy],
  );
  const availableSubcategories = useMemo(
    () => getSubcategories(form.mainCategory),
    [form.mainCategory],
  );

  const loadProducts = useCallback(
    async (pageNumber = pagination.pageNumber) => {
      setIsLoading(true);
      setError(null);

      try {
        const response = await getAdminProducts({
          search,
          isActive: statusFilter === "all" ? null : statusFilter === "active",
          sortBy,
          pageNumber,
          pageSize,
        });

        setProducts(response.items);
        setPagination({
          pageNumber: response.pageNumber,
          totalPages: response.totalPages || 1,
          totalCount: response.totalCount,
          hasPreviousPage: response.hasPreviousPage,
          hasNextPage: response.hasNextPage,
        });
      } catch (loadError) {
        setError(getErrorMessage(loadError, "No se pudieron cargar los productos."));
      } finally {
        setIsLoading(false);
      }
    },
    [pagination.pageNumber, search, sortBy, statusFilter],
  );

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setUser(getStoredSession()?.user ?? null);
      setIsReady(true);
    }, 0);

    return () => window.clearTimeout(timeoutId);
  }, []);

  useEffect(() => {
    if (!isReady || !isAdmin) return undefined;

    const timeoutId = window.setTimeout(() => {
      void loadProducts(pagination.pageNumber);
    }, 0);

    return () => window.clearTimeout(timeoutId);
  }, [isReady, isAdmin, loadProducts, pagination.pageNumber]);

  const resetForm = () => {
    setFormMode("create");
    setForm(createEmptyForm());
  };

  const handleSearchSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setPagination((current) => ({ ...current, pageNumber: 1 }));
    setSearch(searchDraft.trim());
  };

  const handleStatusFilterChange = (nextFilter: StatusFilter) => {
    setPagination((current) => ({ ...current, pageNumber: 1 }));
    setStatusFilter(nextFilter);
  };

  const handleSortChange = (nextSort: AdminProductSort) => {
    setPagination((current) => ({ ...current, pageNumber: 1 }));
    setSortBy(nextSort);
  };

  const handleSortSelect = (nextSort: AdminProductSort) => {
    handleSortChange(nextSort);
    setIsSortOpen(false);
  };

  const handleMainCategorySelect = (category: string) => {
    setForm((current) => ({
      ...current,
      mainCategory: category,
      subcategory: "",
    }));
    setIsMainCategoryOpen(false);
    setIsSubcategoryOpen(false);
  };

  const handleSubcategorySelect = (category: string) => {
    setForm((current) => ({
      ...current,
      subcategory: category,
    }));
    setIsSubcategoryOpen(false);
  };

  const handleEdit = (product: AdminProductItem) => {
    setFormMode("edit");
    setError(null);
    setNotice(null);
    setForm(toForm(product));
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const validationError = validateForm(form);
    if (validationError) {
      setError(validationError);
      return;
    }

    setIsSaving(true);
    setError(null);
    setNotice(null);

    try {
      if (formMode === "edit" && form.id) {
        await updateAdminProduct(form.id, toPayload(form));
        setNotice("Producto actualizado en base de datos.");
      } else {
        await createAdminProduct(toPayload(form));
        setNotice("Producto creado en base de datos.");
      }

      resetForm();
      await loadProducts(formMode === "create" ? 1 : pagination.pageNumber);
    } catch (saveError) {
      setError(getErrorMessage(saveError, "No se pudo guardar el producto."));
    } finally {
      setIsSaving(false);
    }
  };

  const handleToggleActive = async (product: AdminProductItem) => {
    setActionProductId(product.id);
    setError(null);
    setNotice(null);

    try {
      if (product.isActive) {
        await deactivateAdminProduct(product.id);
        setNotice("Producto desactivado en base de datos.");
      } else {
        await activateAdminProduct(product.id);
        setNotice("Producto activado en base de datos.");
      }

      await loadProducts(pagination.pageNumber);
    } catch (actionError) {
      setError(getErrorMessage(actionError, "No se pudo cambiar el estado del producto."));
    } finally {
      setActionProductId(null);
    }
  };

  const handleDelete = async (product: AdminProductItem) => {
    if (product.isActive) return;

    const shouldDelete = window.confirm(`Eliminar el producto ${product.title}?`);
    if (!shouldDelete) return;

    setActionProductId(product.id);
    setError(null);
    setNotice(null);

    try {
      await deleteAdminProduct(product.id);
      setNotice("Producto eliminado en base de datos.");
      resetForm();
      await loadProducts(pagination.pageNumber);
    } catch (deleteError) {
      setError(getErrorMessage(deleteError, "No se pudo eliminar el producto."));
    } finally {
      setActionProductId(null);
    }
  };

  if (!isReady) {
    return (
      <main className="min-h-screen bg-background px-5 pb-16 pt-36 text-foreground sm:px-8 md:pt-24 lg:px-12">
        <section className="mx-auto max-w-6xl border-y border-border/70 bg-paper/55 py-14 text-center">
          <p className="text-sm font-black uppercase tracking-widest text-muted">
            Cargando inventario
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
            Inventario esta disponible solo para la cuenta administradora.
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
              Inventario
            </h1>
            <p className="mt-3 max-w-2xl text-sm font-semibold leading-6 text-muted">
              {pagination.totalCount} productos registrados con stock, compras y favoritos.
            </p>
          </div>
          <Link
            href={routes.adminUsers}
            className="inline-flex h-10 items-center justify-center rounded-full border border-[rgba(53,30,28,0.24)] bg-paper px-5 text-[0.68rem] font-black uppercase tracking-widest text-foreground transition hover:border-accent hover:text-accent focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
          >
            Usuarios
          </Link>
        </section>

        <section className="mt-7 grid gap-4 rounded-[28px] border border-[rgba(53,30,28,0.12)] bg-paper/55 p-3 shadow-[0_14px_32px_rgba(53,30,28,0.05)] xl:grid-cols-[minmax(320px,1fr)_auto] xl:items-center">
          <form
            onSubmit={handleSearchSubmit}
            className="grid min-w-0 gap-2 rounded-full border border-[rgba(53,30,28,0.16)] bg-background/70 p-1.5 sm:grid-cols-[minmax(0,1fr)_auto]"
          >
            <label className="sr-only" htmlFor="admin-product-search">
              Buscar producto
            </label>
            <input
              id="admin-product-search"
              value={searchDraft}
              onChange={(event) => setSearchDraft(event.target.value)}
              placeholder="Nombre, autor, ISBN o ID"
              className="h-10 min-w-0 rounded-full border border-transparent bg-transparent px-4 text-sm font-bold text-foreground outline-none placeholder:text-muted focus:border-[rgba(53,30,28,0.18)] focus:bg-paper"
            />
            <button
              type="submit"
              className="h-10 rounded-full bg-foreground px-6 text-[0.66rem] font-black uppercase tracking-widest text-paper shadow-[0_8px_18px_rgba(53,30,28,0.14)] transition hover:bg-accent focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
            >
              Buscar
            </button>
          </form>

          <div className="grid gap-3 sm:grid-cols-[minmax(0,1fr)_190px] xl:w-[520px]">
            <div className="grid grid-cols-3 rounded-full border border-[rgba(53,30,28,0.16)] bg-background/70 p-1">
              {(["all", "active", "inactive"] as const).map((filter) => (
                <button
                  key={filter}
                  type="button"
                  onClick={() => handleStatusFilterChange(filter)}
                  className={`h-10 rounded-full px-3 text-center text-[0.62rem] font-black uppercase tracking-widest transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent ${
                    statusFilter === filter
                      ? "bg-foreground text-paper shadow-[0_8px_16px_rgba(53,30,28,0.14)]"
                      : "text-muted hover:bg-paper hover:text-foreground"
                  }`}
                >
                  {filter === "all" ? "Todos" : filter === "active" ? "Activos" : "Inactivos"}
                </button>
              ))}
            </div>

            <div
              className="relative"
              onBlur={(event) => {
                if (!event.currentTarget.contains(event.relatedTarget)) {
                  setIsSortOpen(false);
                }
              }}
              onKeyDown={(event) => {
                if (event.key === "Escape") {
                  setIsSortOpen(false);
                }
              }}
            >
              <button
                type="button"
                aria-expanded={isSortOpen}
                aria-haspopup="listbox"
                aria-controls="admin-product-sort-menu"
                onClick={() => setIsSortOpen((isOpen) => !isOpen)}
                className="flex h-12 w-full items-center justify-between gap-3 rounded-full border border-[rgba(53,30,28,0.18)] bg-background/80 px-4 text-left shadow-[0_8px_18px_rgba(53,30,28,0.06)] transition hover:border-[rgba(53,30,28,0.34)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
              >
                <span className="min-w-0">
                  <span className="block text-[0.56rem] font-black uppercase tracking-widest text-muted">
                    Orden
                  </span>
                  <span className="mt-0.5 block truncate text-[0.68rem] font-black uppercase tracking-widest text-foreground">
                    {selectedSort.label}
                  </span>
                </span>
                <span
                  aria-hidden="true"
                  className={`text-sm font-black text-foreground transition ${isSortOpen ? "rotate-180" : ""}`}
                >
                  v
                </span>
              </button>

              {isSortOpen ? (
                <div
                  id="admin-product-sort-menu"
                  role="listbox"
                  aria-label="Ordenar productos"
                  className="absolute right-0 top-[calc(100%+10px)] z-30 w-72 overflow-hidden border border-[rgba(53,30,28,0.16)] bg-paper shadow-[0_18px_42px_rgba(53,30,28,0.16)]"
                >
                  <div className="border-b border-border/70 px-4 py-3">
                    <p className="text-[0.6rem] font-black uppercase tracking-widest text-accent">
                      Ordenar inventario
                    </p>
                  </div>
                  <div className="p-1.5">
                    {sortOptions.map((option) => {
                      const isSelected = option.value === sortBy;

                      return (
                        <button
                          key={option.value}
                          type="button"
                          role="option"
                          aria-selected={isSelected}
                          onClick={() => handleSortSelect(option.value)}
                          className={`grid w-full gap-0.5 rounded-[12px] px-3 py-2.5 text-left transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent ${
                            isSelected
                              ? "bg-foreground text-paper"
                              : "text-foreground hover:bg-background"
                          }`}
                        >
                          <span className="text-[0.68rem] font-black uppercase tracking-widest">
                            {option.label}
                          </span>
                          <span
                            className={`text-xs font-bold ${
                              isSelected ? "text-paper/75" : "text-muted"
                            }`}
                          >
                            {option.detail}
                          </span>
                        </button>
                      );
                    })}
                  </div>
                </div>
              ) : null}
            </div>
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

        <section className="mt-8 grid gap-6 xl:grid-cols-[minmax(0,1fr)_420px]">
          <div className="border border-[rgba(53,30,28,0.14)] bg-paper/65 shadow-[0_16px_36px_rgba(53,30,28,0.05)]">
            <div className="flex flex-col gap-2 border-b border-border/70 px-5 py-4 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <h2 className="text-base font-black text-foreground">
                  Productos
                </h2>
                <p className="mt-1 text-xs font-bold text-muted">
                  Estado, stock, precio y movimiento por libro.
                </p>
              </div>
              <span className="text-[0.62rem] font-black uppercase tracking-widest text-muted">
                {pagination.totalCount} registros
              </span>
            </div>

            {isLoading ? (
              <div className="px-4 py-12 text-center text-sm font-black uppercase tracking-widest text-muted">
                Cargando
              </div>
            ) : products.length === 0 ? (
              <div className="px-4 py-12 text-center text-sm font-bold text-muted">
                No hay productos para este filtro.
              </div>
            ) : (
              <div className="divide-y divide-border/70">
                {products.map((product) => {
                  const isBusy = actionProductId === product.id;

                  return (
                    <article
                      key={product.id}
                      className="grid gap-5 px-5 py-5 lg:grid-cols-[minmax(0,1fr)_minmax(180px,auto)]"
                    >
                      <div className="grid min-w-0 gap-4 sm:grid-cols-[76px_minmax(0,1fr)]">
                        <div className="relative h-28 w-[76px] overflow-hidden rounded-[7px] border border-[rgba(53,30,28,0.18)] bg-card shadow-[0_8px_18px_rgba(53,30,28,0.12)]">
                          <Image
                            src={product.imageUrl?.trim() || fallbackCover}
                            alt={`Portada de ${product.title}`}
                            fill
                            className="object-cover"
                            sizes="76px"
                          />
                        </div>
                        <div className="min-w-0">
                          <div className="flex flex-wrap items-center gap-2.5">
                            <h3 className="min-w-0 max-w-full truncate text-base font-black text-foreground">
                              {product.title}
                            </h3>
                            <span
                              className={`shrink-0 rounded-full border px-2.5 py-1 text-[0.56rem] font-black uppercase tracking-widest ${
                                product.isActive
                                  ? "border-[#b8d8c0] bg-[#eef8f0] text-[#315f3a]"
                                  : "border-accent/30 bg-accent/10 text-accent"
                              }`}
                            >
                              {product.isActive ? "Activo" : "Inactivo"}
                            </span>
                          </div>
                          <p className="mt-1 truncate text-xs font-bold text-muted">
                            {product.authors.join(", ") || "Autor sin registrar"}
                          </p>
                          <div className="mt-3 grid gap-2 text-[0.68rem] font-bold text-muted/85 sm:grid-cols-2">
                            <span className="min-w-0 truncate rounded-full border border-[rgba(53,30,28,0.12)] bg-background/55 px-3 py-1.5">
                              ID {product.id}
                            </span>
                            <span className="min-w-0 truncate rounded-full border border-[rgba(53,30,28,0.12)] bg-background/55 px-3 py-1.5">
                              ISBN {product.isbn || "sin registrar"}
                            </span>
                          </div>
                        </div>
                      </div>

                      <div className="grid gap-4">
                        <div className="grid grid-cols-3 gap-2">
                          <div className="border border-[rgba(53,30,28,0.12)] bg-background/55 px-3 py-2">
                            <p className="text-[0.58rem] font-black uppercase tracking-widest text-muted">
                              Stock
                            </p>
                            <p className="mt-1 text-sm font-black text-foreground">
                              {product.currentStock}
                              <span className="ml-1 text-[0.66rem] font-bold text-muted">
                                / {product.minStock}
                              </span>
                            </p>
                          </div>
                          <div className="border border-[rgba(53,30,28,0.12)] bg-background/55 px-3 py-2">
                            <p className="text-[0.58rem] font-black uppercase tracking-widest text-muted">
                              Compras
                            </p>
                            <p className="mt-1 text-sm font-black text-foreground">
                              {product.purchasedCount}
                            </p>
                          </div>
                          <div className="border border-[rgba(53,30,28,0.12)] bg-background/55 px-3 py-2">
                            <p className="text-[0.58rem] font-black uppercase tracking-widest text-muted">
                              Fav.
                            </p>
                            <p className="mt-1 text-sm font-black text-foreground">
                              {product.favoriteCount}
                            </p>
                          </div>
                        </div>
                        <p className="text-right text-base font-black text-foreground lg:text-left">
                          {formatPrice(product.price)}
                        </p>
                      </div>

                      <div className="flex flex-wrap gap-2 lg:col-span-2 lg:justify-end">
                        <button
                          type="button"
                          onClick={() => handleEdit(product)}
                          disabled={isBusy}
                          className="h-9 min-w-24 rounded-full border border-[rgba(53,30,28,0.2)] bg-paper px-4 text-[0.62rem] font-black uppercase tracking-widest text-foreground transition hover:border-accent hover:text-accent disabled:cursor-not-allowed disabled:opacity-45"
                        >
                          Editar
                        </button>
                        <button
                          type="button"
                          onClick={() => handleToggleActive(product)}
                          disabled={isBusy}
                          className="h-9 min-w-28 rounded-full border border-[rgba(53,30,28,0.2)] bg-paper px-4 text-[0.62rem] font-black uppercase tracking-widest text-foreground transition hover:border-accent hover:text-accent disabled:cursor-not-allowed disabled:opacity-45"
                        >
                          {product.isActive ? "Desactivar" : "Activar"}
                        </button>
                        <button
                          type="button"
                          onClick={() => handleDelete(product)}
                          disabled={product.isActive || isBusy}
                          title={product.isActive ? "Desactiva el producto antes de eliminarlo" : undefined}
                          className="h-9 min-w-24 rounded-full bg-foreground px-4 text-[0.62rem] font-black uppercase tracking-widest text-paper transition hover:bg-accent disabled:cursor-not-allowed disabled:opacity-45"
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
            className="h-fit min-w-0 border border-[rgba(53,30,28,0.16)] bg-paper/75 p-5 shadow-[0_14px_34px_rgba(53,30,28,0.07)]"
          >
            <div className="flex items-start justify-between gap-3">
              <div>
                <p className="text-[0.62rem] font-black uppercase tracking-widest text-accent">
                  {formMode === "create" ? "Nuevo producto" : "Editar producto"}
                </p>
                <h2 className="mt-2 text-xl font-black text-foreground">
                  {formMode === "create" ? "Crear libro" : form.title}
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

            <div className="mt-5 grid gap-4">
              <div className="grid gap-4 sm:grid-cols-[82px_minmax(0,1fr)]">
                <div className="relative h-28 w-[76px] overflow-hidden rounded-[7px] border border-[rgba(53,30,28,0.2)] bg-card shadow-[0_12px_24px_rgba(53,30,28,0.14)]">
                  <Image
                    src={previewImage}
                    alt=""
                    fill
                    className="object-cover"
                    sizes="76px"
                  />
                </div>
                <label className="grid min-w-0 gap-1 text-xs font-black uppercase tracking-widest text-muted">
                  Imagen de portada
                  <input
                    value={form.imageUrl}
                    onChange={(event) => setForm((current) => ({ ...current, imageUrl: event.target.value }))}
                    placeholder="/images/books/portada.webp"
                    className="h-11 w-full min-w-0 rounded-full border border-[rgba(53,30,28,0.2)] bg-background px-4 text-sm font-bold normal-case tracking-normal text-foreground outline-none focus:border-accent"
                  />
                  <span className="text-[0.68rem] font-bold normal-case leading-5 tracking-normal text-muted">
                    {imageFormatText}
                  </span>
                </label>
              </div>

              <label className="grid min-w-0 gap-1 text-xs font-black uppercase tracking-widest text-muted">
                Nombre
                <input
                  value={form.title}
                  onChange={(event) => setForm((current) => ({ ...current, title: event.target.value }))}
                  className="h-11 w-full min-w-0 rounded-full border border-[rgba(53,30,28,0.2)] bg-background px-4 text-sm font-bold normal-case tracking-normal text-foreground outline-none focus:border-accent"
                />
              </label>

              <div className="grid gap-3 sm:grid-cols-2">
                <label className="grid min-w-0 gap-1 text-xs font-black uppercase tracking-widest text-muted">
                  ISBN
                  <input
                    value={form.isbn}
                    onChange={(event) => setForm((current) => ({ ...current, isbn: event.target.value }))}
                    className="h-11 w-full min-w-0 rounded-full border border-[rgba(53,30,28,0.2)] bg-background px-4 text-sm font-bold normal-case tracking-normal text-foreground outline-none focus:border-accent"
                  />
                </label>

                <label className="grid min-w-0 gap-1 text-xs font-black uppercase tracking-widest text-muted">
                  Precio de venta
                  <input
                    inputMode="decimal"
                    value={form.price}
                    onChange={(event) => setForm((current) => ({ ...current, price: event.target.value }))}
                    className="h-11 w-full min-w-0 rounded-full border border-[rgba(53,30,28,0.2)] bg-background px-4 text-sm font-bold normal-case tracking-normal text-foreground outline-none focus:border-accent"
                  />
                </label>
              </div>

              <label className="grid min-w-0 gap-1 text-xs font-black uppercase tracking-widest text-muted">
                Autor
                <input
                  value={form.authorNames}
                  onChange={(event) => setForm((current) => ({ ...current, authorNames: event.target.value }))}
                  placeholder="Autor 1, Autor 2"
                  className="h-11 w-full min-w-0 rounded-full border border-[rgba(53,30,28,0.2)] bg-background px-4 text-sm font-bold normal-case tracking-normal text-foreground outline-none focus:border-accent"
                />
              </label>

              <div className="grid gap-3 sm:grid-cols-2">
                <div
                  className="relative min-w-0"
                  onBlur={(event) => {
                    if (!event.currentTarget.contains(event.relatedTarget)) {
                      setIsMainCategoryOpen(false);
                    }
                  }}
                  onKeyDown={(event) => {
                    if (event.key === "Escape") {
                      setIsMainCategoryOpen(false);
                    }
                  }}
                >
                  <p className="mb-1 text-xs font-black uppercase tracking-widest text-muted">
                    Categoría principal
                  </p>
                  <button
                    type="button"
                    aria-expanded={isMainCategoryOpen}
                    aria-haspopup="listbox"
                    onClick={() => setIsMainCategoryOpen((isOpen) => !isOpen)}
                    className="flex h-11 w-full min-w-0 items-center justify-between gap-3 rounded-full border border-[rgba(53,30,28,0.2)] bg-background px-4 text-left text-sm font-bold text-foreground outline-none transition hover:border-[rgba(53,30,28,0.34)] focus-visible:border-accent focus-visible:ring-2 focus-visible:ring-accent/15"
                  >
                    <span className="truncate">
                      {form.mainCategory || "Seleccionar"}
                    </span>
                    <span
                      aria-hidden="true"
                      className={`text-xs font-black transition ${isMainCategoryOpen ? "rotate-180" : ""}`}
                    >
                      v
                    </span>
                  </button>

                  {isMainCategoryOpen ? (
                    <div
                      role="listbox"
                      aria-label="Categoría principal"
                      className={dropdownListboxClassName}
                    >
                      {categoryGroups.map((group) => (
                        <button
                          key={group.name}
                          type="button"
                          role="option"
                          aria-selected={form.mainCategory === group.name}
                          onClick={() => handleMainCategorySelect(group.name)}
                          className={`w-full rounded-[12px] px-3 py-2.5 text-left text-[0.68rem] font-black uppercase tracking-widest transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent ${
                            form.mainCategory === group.name
                              ? "bg-foreground text-paper"
                              : "text-foreground hover:bg-background"
                          }`}
                        >
                          {group.name}
                        </button>
                      ))}
                    </div>
                  ) : null}
                </div>

                <div
                  className="relative min-w-0"
                  onBlur={(event) => {
                    if (!event.currentTarget.contains(event.relatedTarget)) {
                      setIsSubcategoryOpen(false);
                    }
                  }}
                  onKeyDown={(event) => {
                    if (event.key === "Escape") {
                      setIsSubcategoryOpen(false);
                    }
                  }}
                >
                  <p className="mb-1 text-xs font-black uppercase tracking-widest text-muted">
                    Subgénero
                  </p>
                  <button
                    type="button"
                    aria-expanded={isSubcategoryOpen}
                    aria-haspopup="listbox"
                    disabled={!form.mainCategory || availableSubcategories.length === 0}
                    onClick={() => setIsSubcategoryOpen((isOpen) => !isOpen)}
                    className="flex h-11 w-full min-w-0 items-center justify-between gap-3 rounded-full border border-[rgba(53,30,28,0.2)] bg-background px-4 text-left text-sm font-bold text-foreground outline-none transition hover:border-[rgba(53,30,28,0.34)] focus-visible:border-accent focus-visible:ring-2 focus-visible:ring-accent/15 disabled:cursor-not-allowed disabled:opacity-55"
                  >
                    <span className="truncate">
                      {form.subcategory || (availableSubcategories.length === 0 ? "No aplica" : "Seleccionar")}
                    </span>
                    <span
                      aria-hidden="true"
                      className={`text-xs font-black transition ${isSubcategoryOpen ? "rotate-180" : ""}`}
                    >
                      v
                    </span>
                  </button>

                  {isSubcategoryOpen ? (
                    <div
                      role="listbox"
                      aria-label="Subgénero"
                      className={dropdownListboxClassName}
                    >
                      {availableSubcategories.map((category) => (
                        <button
                          key={category}
                          type="button"
                          role="option"
                          aria-selected={form.subcategory === category}
                          onClick={() => handleSubcategorySelect(category)}
                          className={`w-full rounded-[12px] px-3 py-2.5 text-left text-[0.68rem] font-black uppercase tracking-widest transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent ${
                            form.subcategory === category
                              ? "bg-foreground text-paper"
                              : "text-foreground hover:bg-background"
                          }`}
                        >
                          {category}
                        </button>
                      ))}
                    </div>
                  ) : null}
                </div>
              </div>

              <label className="grid min-w-0 gap-1 text-xs font-black uppercase tracking-widest text-muted">
                Editorial
                <input
                  value={form.publisherName}
                  onChange={(event) => setForm((current) => ({ ...current, publisherName: event.target.value }))}
                  className="h-11 w-full min-w-0 rounded-full border border-[rgba(53,30,28,0.2)] bg-background px-4 text-sm font-bold normal-case tracking-normal text-foreground outline-none focus:border-accent"
                />
              </label>

              <div className="grid gap-3 sm:grid-cols-3">
                <label className="grid min-w-0 gap-1 text-xs font-black uppercase tracking-widest text-muted">
                  Año
                  <input
                    inputMode="numeric"
                    value={form.publicationYear}
                    onChange={(event) => setForm((current) => ({ ...current, publicationYear: event.target.value.replace(/\D/g, "") }))}
                    placeholder="2026"
                    className="h-11 w-full min-w-0 rounded-full border border-[rgba(53,30,28,0.2)] bg-background px-4 text-sm font-bold normal-case tracking-normal text-foreground outline-none focus:border-accent"
                  />
                </label>

                <label className="grid min-w-0 gap-1 text-xs font-black uppercase tracking-widest text-muted">
                  Stock
                  <input
                    inputMode="numeric"
                    value={form.currentStock}
                    onChange={(event) => setForm((current) => ({ ...current, currentStock: event.target.value.replace(/\D/g, "") }))}
                    className="h-11 w-full min-w-0 rounded-full border border-[rgba(53,30,28,0.2)] bg-background px-4 text-sm font-bold normal-case tracking-normal text-foreground outline-none focus:border-accent"
                  />
                </label>

                <label className="grid min-w-0 gap-1 text-xs font-black uppercase tracking-widest text-muted">
                  Stock mínimo
                  <input
                    inputMode="numeric"
                    value={form.minStock}
                    onChange={(event) => setForm((current) => ({ ...current, minStock: event.target.value.replace(/\D/g, "") }))}
                    className="h-11 w-full min-w-0 rounded-full border border-[rgba(53,30,28,0.2)] bg-background px-4 text-sm font-bold normal-case tracking-normal text-foreground outline-none focus:border-accent"
                  />
                </label>
              </div>

              <label className="grid min-w-0 gap-1 text-xs font-black uppercase tracking-widest text-muted">
                Sinopsis del libro
                <textarea
                  value={form.description}
                  onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))}
                  rows={4}
                  placeholder="Resumen breve que verá el usuario en el detalle del libro."
                  className={textareaClassName}
                />
              </label>
            </div>

            <button
              type="submit"
              disabled={isSaving}
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
              disabled={!pagination.hasPreviousPage || isLoading}
              onClick={() => setPagination((current) => ({ ...current, pageNumber: Math.max(1, current.pageNumber - 1) }))}
              className="h-10 rounded-full border border-[rgba(53,30,28,0.2)] bg-paper px-5 text-[0.62rem] font-black uppercase tracking-widest text-foreground transition hover:border-accent hover:text-accent disabled:cursor-not-allowed disabled:opacity-45"
            >
              Anterior
            </button>
            <button
              type="button"
              disabled={!pagination.hasNextPage || isLoading}
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
