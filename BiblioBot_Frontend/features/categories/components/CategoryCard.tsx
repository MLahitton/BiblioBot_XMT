"use client";

import { motion, useReducedMotion } from "framer-motion";
import Image from "next/image";
import type { Category } from "../types/category.types";

type CategoryCardProps = {
  category: Category;
  revealDelay?: number;
};

export function CategoryCard({
  category,
  revealDelay = 0,
}: CategoryCardProps) {
  const shouldReduceMotion = useReducedMotion();

  return (
    <motion.article
      className="group flex h-full flex-col rounded-xl border border-border bg-card p-5 backdrop-blur-xl transition-colors duration-300 hover:border-accent/50 hover:bg-white/[0.1]"
      initial={shouldReduceMotion ? { opacity: 1 } : { opacity: 0, y: 22 }}
      whileInView={
        shouldReduceMotion ? { opacity: 1 } : { opacity: 1, y: 0 }
      }
      whileHover={shouldReduceMotion ? undefined : { y: -5 }}
      transition={{ delay: revealDelay, duration: 0.62, ease: [0.22, 1, 0.36, 1] }}
      viewport={{ once: true, amount: 0.32, margin: "0px 0px -10% 0px" }}
    >
      <div className="flex h-12 w-12 items-center justify-center rounded-xl border border-border bg-accent/10 transition group-hover:border-accent/50">
        <Image src={category.icon} alt="" width={27} height={27} aria-hidden />
      </div>
      <h3 className="mt-5 text-lg font-semibold text-foreground">
        {category.name}
      </h3>
      <p className="mt-2 flex-1 text-sm leading-6 text-muted">
        {category.description}
      </p>
      <p className="mt-5 text-sm font-medium text-accent-soft">
        {category.totalBooks} libros disponibles
      </p>
    </motion.article>
  );
}
