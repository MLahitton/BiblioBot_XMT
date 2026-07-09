export const defaultCurrency = "COP";

export const defaultPriceLocale = "es-CO";

export const priceFormatOptions: Intl.NumberFormatOptions = {
  style: "currency",
  currency: defaultCurrency,
  currencyDisplay: "code",
  minimumFractionDigits: 0,
  maximumFractionDigits: 0,
};
