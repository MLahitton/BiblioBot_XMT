export const defaultCurrency = "USD";

export const defaultPriceLocale = "es-CO";

export const priceFormatOptions: Intl.NumberFormatOptions = {
  style: "currency",
  currency: defaultCurrency,
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
};
