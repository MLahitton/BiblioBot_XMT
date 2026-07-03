"use client";

import { useRef } from "react";
import {
  useReducedMotion,
  useScroll,
  useSpring,
  useTransform,
} from "framer-motion";
import type { Book } from "@/features/books/types/book.types";
import type { Category } from "@/features/categories/types/category.types";
import { BenefitsSection } from "./BenefitsSection";
import { CategorySection } from "./CategorySection";
import { FeaturedBooksSection } from "./FeaturedBooksSection";
import { FinalCtaSection } from "./FinalCtaSection";
import { FloatingBook } from "./FloatingBook";
import { HeroSection } from "./HeroSection";

type ParallaxBookExperienceProps = {
  books: Book[];
  categories: Category[];
};

export function ParallaxBookExperience({
  books,
  categories,
}: ParallaxBookExperienceProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const shouldReduceMotion = useReducedMotion();
  const { scrollYProgress } = useScroll({
    target: containerRef,
    offset: ["start start", "end end"],
  });

  const smoothProgress = useSpring(scrollYProgress, {
    stiffness: 80,
    damping: 24,
    mass: 0.26,
  });

  const x = useTransform(
    smoothProgress,
    [0, 0.22, 0.48, 0.72, 1],
    shouldReduceMotion
      ? ["0%", "0%", "0%", "0%", "0%"]
      : ["0%", "12%", "-13%", "9%", "-4%"],
  );
  const y = useTransform(
    smoothProgress,
    [0, 0.22, 0.48, 0.72, 1],
    shouldReduceMotion ? [0, 0, 0, 0, 0] : [0, -72, 90, 168, 24],
  );
  const scale = useTransform(
    smoothProgress,
    [0, 0.24, 0.5, 0.76, 1],
    shouldReduceMotion ? [0.92, 0.92, 0.92, 0.92, 0.92] : [1, 0.88, 0.68, 0.56, 0.82],
  );
  const rotateZ = useTransform(
    smoothProgress,
    [0, 0.25, 0.52, 0.78, 1],
    shouldReduceMotion ? [-4, -4, -4, -4, -4] : [-5, 6, -8, 3, -2],
  );
  const rotateY = useTransform(
    smoothProgress,
    [0, 0.35, 0.68, 1],
    shouldReduceMotion ? [-10, -10, -10, -10] : [-12, -22, 16, -8],
  );
  const opacity = useTransform(
    smoothProgress,
    [0, 0.6, 0.78, 1],
    shouldReduceMotion ? [0.9, 0.9, 0.9, 0.9] : [1, 0.92, 0.42, 0.72],
  );

  return (
    <div ref={containerRef} className="relative overflow-x-clip">
      <div className="pointer-events-none absolute inset-0 z-10 overflow-hidden">
        <div className="sticky top-0 h-screen">
          <FloatingBook
            className="absolute right-1/2 top-[420px] w-[min(72vw,320px)] translate-x-1/2 transform-gpu will-change-transform sm:right-[8%] sm:top-[118px] sm:w-[min(46vw,430px)] sm:translate-x-0 lg:right-[8%] lg:top-[120px] lg:w-[470px]"
            style={{
              x,
              y,
              scale,
              rotateZ,
              rotateY,
              opacity,
              transformPerspective: 1000,
            }}
          />
        </div>
      </div>
      <main className="relative z-20">
        <HeroSection />
        <CategorySection categories={categories} />
        <FeaturedBooksSection books={books} />
        <BenefitsSection />
        <FinalCtaSection />
      </main>
    </div>
  );
}
