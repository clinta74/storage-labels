import { format } from "date-fns";

export const dateToString = (date: Date) => format(date, 'yyyy-MM-dd');