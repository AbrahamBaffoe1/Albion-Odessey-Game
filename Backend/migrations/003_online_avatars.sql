begin;
alter table public.student_profiles add column if not exists avatar_skin integer not null default 0 check (avatar_skin between 0 and 4);
alter table public.student_profiles add column if not exists avatar_outfit integer not null default 0 check (avatar_outfit between 0 and 4);
alter table public.student_profiles add column if not exists avatar_hair integer not null default 0 check (avatar_hair between 0 and 2);
alter table public.student_profiles add column if not exists avatar_backpack boolean not null default true;
alter table public.student_profiles add column if not exists avatar_ready boolean not null default false;
update public.student_profiles set
 avatar_skin=((hashtextextended(id::text,1)&2147483647)%5)::int,
 avatar_outfit=((hashtextextended(id::text,2)&2147483647)%5)::int,
 avatar_hair=((hashtextextended(id::text,3)&2147483647)%3)::int,
 avatar_ready=true where not avatar_ready;
create or replace function public.create_student_profile() returns trigger
language plpgsql security definer set search_path='' as $$
begin
 insert into public.student_profiles(id,avatar_skin,avatar_outfit,avatar_hair,avatar_ready)
 values(new.id,((hashtextextended(new.id::text,1)&2147483647)%5)::int,
 ((hashtextextended(new.id::text,2)&2147483647)%5)::int,
 ((hashtextextended(new.id::text,3)&2147483647)%3)::int,true) on conflict do nothing;
 return new;
end; $$;
grant update(avatar_skin,avatar_outfit,avatar_hair,avatar_backpack,avatar_ready) on public.student_profiles to authenticated;
-- Existing owner-only RLS remains in force. Appearance grants confer no school role or currency authority.
commit;
