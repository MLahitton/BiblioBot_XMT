"use client";

import { motion, useReducedMotion } from "framer-motion";
import Image from "next/image";
import type { Category } from "../types/category.types";

const categoryPalette: Record<
  string,
  {
    surface: string;
    text: string;
    icon: string;
    shadow: string;
  }
> = {
  fiction: {
    surface: "bg-[#733635]/12 hover:bg-[#733635]/18",
    text: "text-[#733635]",
    icon: "bg-[#733635] border-[#733635]/20",
    shadow: "hover:shadow-[0_10px_22px_rgba(115,54,53,0.14)]",
  },
  business: {
    surface: "bg-[#351E1C]/10 hover:bg-[#351E1C]/15",
    text: "text-[#351E1C]",
    icon: "bg-[#351E1C] border-[#351E1C]/20",
    shadow: "hover:shadow-[0_10px_22px_rgba(53,30,28,0.14)]",
  },
  technology: {
    surface: "bg-[#A0C9CB]/42 hover:bg-[#A0C9CB]/55",
    text: "text-[#351E1C]",
    icon: "bg-[#A0C9CB] border-[#A0C9CB]",
    shadow: "hover:shadow-[0_10px_22px_rgba(160,201,203,0.32)]",
  },
  science: {
    surface: "bg-[#ECECDC] hover:bg-[#E4E4D1]",
    text: "text-[#351E1C]",
    icon: "bg-[#ECECDC] border-[#D8D8C4]",
    shadow: "hover:shadow-[0_10px_22px_rgba(53,30,28,0.08)]",
  },
  fantasy: {
    surface: "bg-[#FF6037]/14 hover:bg-[#FF6037]/22",
    text: "text-[#733635]",
    icon: "bg-[#FF6037] border-[#FF6037]/20",
    shadow: "hover:shadow-[0_10px_22px_rgba(255,96,55,0.18)]",
  },
  "personal-growth": {
    surface: "bg-[#F5F4ED] hover:bg-[#ECECDC]",
    text: "text-[#733635]",
    icon: "bg-[#F5F4ED] border-[#DCD8C8]",
    shadow: "hover:shadow-[0_10px_22px_rgba(115,54,53,0.1)]",
  },
  history: {
    surface: "bg-[#733635]/15 hover:bg-[#733635]/22",
    text: "text-[#733635]",
    icon: "bg-[#733635] border-[#733635]/20",
    shadow: "hover:shadow-[0_10px_22px_rgba(115,54,53,0.16)]",
  },
  art: {
    surface: "bg-[#A0C9CB]/38 hover:bg-[#A0C9CB]/52",
    text: "text-[#351E1C]",
    icon: "bg-[#A0C9CB] border-[#A0C9CB]",
    shadow: "hover:shadow-[0_10px_22px_rgba(160,201,203,0.3)]",
  },
};

type CategoryCardProps = {
  category: Category;
  revealDelay?: number;
};

export function CategoryCard({
  category,
  revealDelay = 0,
}: CategoryCardProps) {
  const shouldReduceMotion = useReducedMotion();
  const palette = categoryPalette[category.id] ?? categoryPalette.science;
  const shouldInvertIcon =
    palette.icon.includes("#351E1C") ||
    palette.icon.includes("#733635") ||
    palette.icon.includes("#FF6037");

  return (
    <motion.a
      id={category.slug}
      href={`#${category.slug}`}
      className={`flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-bold transition ${palette.surface} ${palette.text} ${palette.shadow} focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent`}
      initial={shouldReduceMotion ? { opacity: 1 } : { opacity: 0, y: 10 }}
      whileInView={shouldReduceMotion ? { opacity: 1 } : { opacity: 1, y: 0 }}
      transition={{ delay: revealDelay, duration: 0.38, ease: "easeOut" }}
      viewport={{ once: true, amount: 0.4 }}
    >
      <span
        className={`flex h-6 w-6 items-center justify-center rounded-md border ${palette.icon}`}
      >
        <Image
          src={category.icon}
          alt=""
          width={14}
          height={14}
          aria-hidden
          className={shouldInvertIcon ? "invert" : ""}
        />
      </span>
      <span className="min-w-0 flex-1 truncate">{category.name}</span>
    </motion.a>
  );
}
