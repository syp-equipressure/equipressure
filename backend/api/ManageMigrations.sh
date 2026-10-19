#!/usr/bin/env bash
#
# EF Core Migration Management
#
# Usage:
#   ./migrations.sh [-m <migration-project>] [-s <startup-project>]
#
# Projects can also be set via environment variables:
#   MIGRATION_PROJECT=... STARTUP_PROJECT=... ./migrations.sh

set -uo pipefail

MIGRATION_PROJECT="${MIGRATION_PROJECT:-./EquiApi.Persistence/EquiApi.Persistence.csproj}"
STARTUP_PROJECT="${STARTUP_PROJECT:-./EquiApi/EquiApi.csproj}"

while getopts ":m:s:h" opt; do
    case "$opt" in
        m) MIGRATION_PROJECT="$OPTARG" ;;
        s) STARTUP_PROJECT="$OPTARG" ;;
        h)
            echo "Usage: $0 [-m <migration-project>] [-s <startup-project>]"
            exit 0
            ;;
        \?)
            echo "Unknown option: -$OPTARG" >&2
            exit 1
            ;;
        :)
            echo "Option -$OPTARG requires an argument." >&2
            exit 1
            ;;
    esac
done

# Migration names start with a 14-digit timestamp. This skips [dbg] / build log lines.
MIGRATION_NAME_REGEX='^[0-9]{14}_.+'

# Prints migration names (one per line), without the " (Pending)" suffix EF may append.
get_migrations() {
    dotnet ef migrations list \
        --project "$MIGRATION_PROJECT" \
        --startup-project "$STARTUP_PROJECT" 2>/dev/null \
        | grep -E "$MIGRATION_NAME_REGEX" \
        | sed -E 's/[[:space:]]+\(Pending\)$//' \
        || true
}

add_migration() {
    local migration_name="${1:-}"

    if [[ -z "${migration_name// }" ]]; then
        migration_name="Initial"
    fi

    echo
    echo "Adding migration '$migration_name'..."
    dotnet ef migrations add "$migration_name" \
        --project "$MIGRATION_PROJECT" \
        --startup-project "$STARTUP_PROJECT"
}

update_database() {
    echo
    echo "Updating the database..."
    dotnet ef database update \
        --project "$MIGRATION_PROJECT" \
        --startup-project "$STARTUP_PROJECT"
}

script_migration() {
    local from_migration="${1:-0}"
    local to_migration="${2:-}"
    local output_path="${3:-./migration.sql}"

    [[ -z "${from_migration// }" ]] && from_migration="0"
    [[ -z "${output_path// }" ]] && output_path="./migration.sql"

    if [[ -z "${to_migration// }" ]]; then
        # Same filtering as list_migrations, take the last (latest) one
        to_migration="$(get_migrations | tail -n 1)"
    fi

    if [[ -z "$to_migration" ]]; then
        echo "No migrations found. Nothing to script." >&2
        return 1
    fi

    echo
    echo "Generating SQL script from migration '$from_migration' to '$to_migration'..."
    dotnet ef migrations script "$from_migration" "$to_migration" \
        --project "$MIGRATION_PROJECT" \
        --startup-project "$STARTUP_PROJECT" \
        --output "$output_path" || return $?

    echo "SQL script written to $output_path"
}

list_migrations() {
    echo
    echo "Available migrations:"
    echo "----------------------------------"

    local i=0
    local migration
    while IFS= read -r migration; do
        [[ -z "$migration" ]] && continue
        echo "$i. $migration"
        i=$((i + 1))
    done < <(get_migrations)

    if [[ $i -eq 0 ]]; then
        echo "No migrations found."
    fi
    echo "----------------------------------"
}

echo "=================================="
echo "   EF Core Migration Management   "
echo "=================================="
echo "1) Add Migration"
echo "2) Update Database"
echo "3) Add + Update"
echo "4) Generate SQL Migration Script"
echo "5) List Migrations"
echo "----------------------------------"

read -r -p "Please enter 1, 2, 3, 4 or 5: " choice

case "$choice" in
    1)
        read -r -p "Enter the migration name (leave blank for 'Initial'): " migration_name
        add_migration "$migration_name"
        ;;
    2)
        update_database
        ;;
    3)
        read -r -p "Enter the migration name (leave blank for 'Initial'): " migration_name
        if add_migration "$migration_name"; then
            echo "--------------------------------------------------------------------"
            update_database
        else
            echo "--------------------------------------------------------------------"
            echo "Migration failed. Database update skipped."
        fi
        ;;
    4)
        echo
        echo "Generating SQL Migration Script..."
        list_migrations
        read -r -p "Enter the start migration name (leave blank for '0'): " from_migration
        read -r -p "Enter the target migration name (leave blank for latest): " to_migration
        read -r -p "Enter output SQL file path (leave blank for './migration.sql'): " output_path
        script_migration "$from_migration" "$to_migration" "$output_path"
        ;;
    5)
        list_migrations
        ;;
    *)
        echo
        echo "Invalid selection. Exiting..."
        ;;
esac

echo
echo "Done!"