begin;

create table if not exists public.student_profiles (
  id uuid primary key references auth.users(id) on delete cascade,
  display_name text not null default 'Student' check (char_length(display_name) between 1 and 24 and display_name !~ '[<>[:cntrl:]]'),
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now()
);
alter table public.student_profiles enable row level security;
revoke all on public.student_profiles from anon, authenticated;
grant select on public.student_profiles to authenticated;
grant update(display_name) on public.student_profiles to authenticated;
drop policy if exists own_profile_read on public.student_profiles;
create policy own_profile_read on public.student_profiles for select to authenticated
using (id = (select auth.uid()));
drop policy if exists own_profile_update on public.student_profiles;
create policy own_profile_update on public.student_profiles for update to authenticated
using (id = (select auth.uid())) with check (id = (select auth.uid()));

create or replace function public.create_student_profile() returns trigger
language plpgsql security definer set search_path = '' as $$
begin
  insert into public.student_profiles(id) values (new.id) on conflict do nothing;
  return new;
end;
$$;
revoke all on function public.create_student_profile() from public, anon, authenticated;
drop trigger if exists create_student_profile on auth.users;
create trigger create_student_profile after insert on auth.users
for each row execute function public.create_student_profile();
insert into public.student_profiles(id) select id from auth.users on conflict do nothing;

create or replace function public.touch_student_profile() returns trigger
language plpgsql set search_path = '' as $$
begin new.updated_at = now(); return new; end;
$$;
drop trigger if exists touch_student_profile on public.student_profiles;
create trigger touch_student_profile before update on public.student_profiles
for each row execute function public.touch_student_profile();

-- Deliberately no email, campus membership, teacher role, or client-owned currency
-- here. Email ownership is verified by Auth; enrollment permissions need a server.
commit;
