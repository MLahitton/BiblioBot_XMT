export type Book = {
  id: string;
  title: string;
  author: string;
  category: string;
  price: number;
  previousPrice?: number;
  rating: number;
  image: string;
  badge?: string;
  description: string;
  stock: number;
  slug: string;
  isbn?: string | null;
  publisher?: string | null;
};

export type BookApiResponse = {
  id: string;
  title: string;
  isbn?: string | null;
  description?: string | null;
  publisherName?: string | null;
  publicationYear?: number | null;
  language?: string | null;
  imageUrl?: string | null;
  price: number;
  authors: string[];
  categories: string[];
  totalStock: number;
};

export type PagedBooksApiResponse = {
  items: BookApiResponse[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
};
