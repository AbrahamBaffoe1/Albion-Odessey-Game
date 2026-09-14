begin;
alter table public.student_profiles drop constraint if exists student_profile_name_not_blank;
alter table public.student_profiles add constraint student_profile_name_not_blank
  check (char_length(btrim(display_name)) > 0);
commit;
