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
};

export type BookApiResponse = {
  id: string;
  title: string;
  author: string;
  category: string;
  price: number;
  previous_price?: number;
  rating: number;
  image_url: string;
  badge?: string;
  description: string;
  stock: number;
  slug: string;
};
