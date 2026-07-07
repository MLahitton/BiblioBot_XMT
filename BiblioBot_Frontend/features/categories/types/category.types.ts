export type Category = {
  id: string;
  name: string;
  description: string;
  icon: string;
  slug: string;
  totalBooks: number;
};

export type CategoryApiResponse = {
  id: string;
  name: string;
  isActive: boolean;
};
