import type { ObjectId } from "mongodb";
import { z } from "zod";

export const PROFILE_BIO_MAX = 500;
const EARLIEST_BIRTH_YEAR = 1900;

/** Shape of a document in the `profile` collection. `_id` is the owning user's id. */
export interface ProfileDocument {
    _id: ObjectId;
    bio: string;
    birthday: Date;
    updatedAt: Date;
}

export const upsertProfileSchema = z
    .object({
        bio: z.string().trim().max(PROFILE_BIO_MAX).default(""),
        birthday_day: z.coerce.number().int().min(1).max(31),
        birthday_month: z.coerce.number().int().min(1).max(12),
        birthday_year: z.coerce.number().int().min(EARLIEST_BIRTH_YEAR).max(new Date().getFullYear()),
    })
    .transform(({ bio, birthday_day, birthday_month, birthday_year }) => ({
        bio,
        birthday: new Date(Date.UTC(birthday_year, birthday_month - 1, birthday_day)),
        // Keep the parts so the refinement below can reject e.g. Feb 30.
        parts: { day: birthday_day, month: birthday_month, year: birthday_year },
    }))
    .refine(
        ({ birthday, parts }) =>
            birthday.getUTCFullYear() === parts.year
            && birthday.getUTCMonth() === parts.month - 1
            && birthday.getUTCDate() === parts.day,
        { message: "Birthday is not a real calendar date", path: ["birthday_day"] },
    )
    .transform(({ bio, birthday }) => ({ bio, birthday }));
export type UpsertProfileInput = z.infer<typeof upsertProfileSchema>;
