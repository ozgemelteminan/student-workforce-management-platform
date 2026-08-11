import * as TabsPrimitive from '@radix-ui/react-tabs'
import { forwardRef, type ComponentPropsWithoutRef, type ElementRef } from 'react'
import { cn } from '../../lib/utils/cn'

export const Tabs = TabsPrimitive.Root

export const TabsList = forwardRef<ElementRef<typeof TabsPrimitive.List>, ComponentPropsWithoutRef<typeof TabsPrimitive.List>>(
  ({ className, ...props }, ref) => <TabsPrimitive.List ref={ref} className={cn('flex gap-1 overflow-x-auto border-b border-border', className)} {...props} />,
)
TabsList.displayName = TabsPrimitive.List.displayName

export const TabsTrigger = forwardRef<ElementRef<typeof TabsPrimitive.Trigger>, ComponentPropsWithoutRef<typeof TabsPrimitive.Trigger>>(
  ({ className, ...props }, ref) => (
    <TabsPrimitive.Trigger
      ref={ref}
      className={cn('border-b-2 border-transparent px-3 py-2 text-sm font-medium text-text-secondary outline-none transition-colors hover:text-text-primary data-[state=active]:border-brand data-[state=active]:text-text-primary', className)}
      {...props}
    />
  ),
)
TabsTrigger.displayName = TabsPrimitive.Trigger.displayName

export const TabsContent = forwardRef<ElementRef<typeof TabsPrimitive.Content>, ComponentPropsWithoutRef<typeof TabsPrimitive.Content>>(
  ({ className, ...props }, ref) => <TabsPrimitive.Content ref={ref} className={cn('pt-4 focus-visible:outline-brand', className)} {...props} />,
)
TabsContent.displayName = TabsPrimitive.Content.displayName
