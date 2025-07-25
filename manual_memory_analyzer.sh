#!/bin/bash
# Manual Memory Analysis Script for GitHub Copilot Processes
# This script performs memory analysis without gcore using /proc filesystem

set -e

ANALYSIS_DIR="manual_memory_analysis_$(date +%s)"
mkdir -p "$ANALYSIS_DIR"

echo "=== Manual Memory Analysis Started ===" | tee "$ANALYSIS_DIR/analysis.log"
echo "Timestamp: $(date)" | tee -a "$ANALYSIS_DIR/analysis.log"

# Phase 0: Document baseline environment
echo "=== PHASE 0: BASELINE DOCUMENTATION ===" | tee -a "$ANALYSIS_DIR/analysis.log"

# Document environment variables with secrets
echo "Environment tokens:" > "$ANALYSIS_DIR/baseline_env.txt"
env | grep -iE "(token|key|secret|auth|pass)" >> "$ANALYSIS_DIR/baseline_env.txt" 2>/dev/null || true

# Document GitHub processes
echo "GitHub-related processes:" > "$ANALYSIS_DIR/baseline_processes.txt"
ps auxf | grep -E "(github|copilot|node|runner)" | grep -v grep >> "$ANALYSIS_DIR/baseline_processes.txt"

# Document known user tokens
echo "Known user tokens:" > "$ANALYSIS_DIR/user_tokens.txt"
echo "GIT_TOKEN: $GIT_TOKEN" >> "$ANALYSIS_DIR/user_tokens.txt" 2>/dev/null || echo "GIT_TOKEN not set" >> "$ANALYSIS_DIR/user_tokens.txt"

# Phase 1: Identify target processes
echo "=== PHASE 1: TARGET PROCESS IDENTIFICATION ===" | tee -a "$ANALYSIS_DIR/analysis.log"

TARGET_PIDS=($(ps -eo pid,comm | grep -E "(node|runner|github)" | grep -v grep | awk '{print $1}'))
echo "Target PIDs: ${TARGET_PIDS[@]}" | tee -a "$ANALYSIS_DIR/analysis.log"

# Phase 2: Extract memory strings from each process
echo "=== PHASE 2: MEMORY STRING EXTRACTION ===" | tee -a "$ANALYSIS_DIR/analysis.log"

for pid in "${TARGET_PIDS[@]}"; do
    if [ -r "/proc/$pid/maps" ] && [ -r "/proc/$pid/mem" ]; then
        echo "Analyzing PID $pid..." | tee -a "$ANALYSIS_DIR/analysis.log"
        
        # Get process info
        ps -o pid,ppid,comm,cmd -p "$pid" > "$ANALYSIS_DIR/process_${pid}_info.txt" 2>/dev/null || true
        
        # Get memory mappings
        cp "/proc/$pid/maps" "$ANALYSIS_DIR/process_${pid}_maps.txt" 2>/dev/null || true
        
        # Extract strings from process memory using multiple methods
        echo "Extracting strings from PID $pid memory..."
        
        # Method 1: Use strings on /proc/pid/mem (may not work due to permissions)
        timeout 30 strings "/proc/$pid/mem" > "$ANALYSIS_DIR/process_${pid}_strings.txt" 2>/dev/null || true
        
        # Method 2: Use /proc/pid/environ for environment variables
        tr '\0' '\n' < "/proc/$pid/environ" > "$ANALYSIS_DIR/process_${pid}_environ.txt" 2>/dev/null || true
        
        # Method 3: Use /proc/pid/cmdline
        tr '\0' ' ' < "/proc/$pid/cmdline" > "$ANALYSIS_DIR/process_${pid}_cmdline.txt" 2>/dev/null || true
        
        # Method 4: Search for readable memory regions and extract strings
        if [ -r "/proc/$pid/maps" ]; then
            while read -r line; do
                if [[ "$line" == *"r-x"* ]] || [[ "$line" == *"rw-"* ]]; then
                    # Extract readable regions (simplified approach)
                    echo "$line" >> "$ANALYSIS_DIR/process_${pid}_readable_regions.txt"
                fi
            done < "/proc/$pid/maps" 2>/dev/null || true
        fi
        
        echo "Completed analysis for PID $pid" | tee -a "$ANALYSIS_DIR/analysis.log"
    else
        echo "Cannot access memory for PID $pid (permissions)" | tee -a "$ANALYSIS_DIR/analysis.log"
    fi
done

# Phase 3: Search for secrets in extracted data
echo "=== PHASE 3: SECRET PATTERN ANALYSIS ===" | tee -a "$ANALYSIS_DIR/analysis.log"

# GitHub token patterns
GITHUB_PATTERNS=(
    "ghp_[A-Za-z0-9]{36}"  # Personal Access Token
    "ghs_[A-Za-z0-9]{36}"  # Session Token  
    "ghu_[A-Za-z0-9]{36}"  # User Token
    "ghr_[A-Za-z0-9]{36}"  # App Token
    "ghi_[A-Za-z0-9]{36}"  # Installation Token
    "gho_[A-Za-z0-9]{36}"  # OAuth Token
)

echo "Searching for GitHub tokens..." | tee -a "$ANALYSIS_DIR/analysis.log"

# Create consolidated search results
echo "=== FOUND TOKENS ===" > "$ANALYSIS_DIR/found_tokens.txt"

for pattern in "${GITHUB_PATTERNS[@]}"; do
    echo "Searching for pattern: $pattern" | tee -a "$ANALYSIS_DIR/analysis.log"
    
    # Search in all extracted strings
    for strings_file in "$ANALYSIS_DIR"/process_*_strings.txt; do
        if [ -f "$strings_file" ]; then
            pid=$(echo "$strings_file" | sed 's/.*process_\([0-9]*\)_strings.txt/\1/')
            echo "=== PID $pid - Pattern $pattern ===" >> "$ANALYSIS_DIR/found_tokens.txt"
            grep -E "$pattern" "$strings_file" >> "$ANALYSIS_DIR/found_tokens.txt" 2>/dev/null || true
        fi
    done
    
    # Search in environment variables
    for env_file in "$ANALYSIS_DIR"/process_*_environ.txt; do
        if [ -f "$env_file" ]; then
            pid=$(echo "$env_file" | sed 's/.*process_\([0-9]*\)_environ.txt/\1/')
            echo "=== PID $pid ENV - Pattern $pattern ===" >> "$ANALYSIS_DIR/found_tokens.txt"
            grep -E "$pattern" "$env_file" >> "$ANALYSIS_DIR/found_tokens.txt" 2>/dev/null || true
        fi
    done
done

# Phase 4: Analyze and categorize found tokens
echo "=== PHASE 4: TOKEN ANALYSIS AND CATEGORIZATION ===" | tee -a "$ANALYSIS_DIR/analysis.log"

echo "=== TOKEN ANALYSIS RESULTS ===" > "$ANALYSIS_DIR/token_analysis.txt"

# Extract unique tokens
FOUND_TOKENS=($(grep -E "gh[spu]_[A-Za-z0-9]{36}" "$ANALYSIS_DIR/found_tokens.txt" | sort -u))

echo "Total unique GitHub tokens found: ${#FOUND_TOKENS[@]}" | tee -a "$ANALYSIS_DIR/analysis.log"
echo "Unique tokens: ${#FOUND_TOKENS[@]}" >> "$ANALYSIS_DIR/token_analysis.txt"

for token in "${FOUND_TOKENS[@]}"; do
    echo "=== ANALYZING TOKEN: ${token:0:10}... ===" >> "$ANALYSIS_DIR/token_analysis.txt"
    
    # Determine token type
    if [[ "$token" == ghp_* ]]; then
        token_type="Personal Access Token"
        risk_level="MEDIUM"
    elif [[ "$token" == ghs_* ]]; then
        token_type="Session Token"
        risk_level="HIGH"
    elif [[ "$token" == ghu_* ]]; then
        token_type="User Token"
        risk_level="MEDIUM"
    elif [[ "$token" == ghr_* ]]; then
        token_type="App Token"
        risk_level="HIGH"
    else
        token_type="Unknown"
        risk_level="UNKNOWN"
    fi
    
    # Check if it's a known user token
    ownership="UNKNOWN"
    if grep -q "$token" "$ANALYSIS_DIR/user_tokens.txt" 2>/dev/null; then
        ownership="USER_KNOWN"
    elif [[ "$token" == ghs_* ]]; then
        ownership="GITHUB_INTERNAL"
    elif [[ "$token" == ghp_* ]]; then
        ownership="USER_UNKNOWN"
    fi
    
    echo "Token Type: $token_type" >> "$ANALYSIS_DIR/token_analysis.txt"
    echo "Ownership: $ownership" >> "$ANALYSIS_DIR/token_analysis.txt"
    echo "Risk Level: $risk_level" >> "$ANALYSIS_DIR/token_analysis.txt"
    
    # Find where this token was discovered
    echo "Found in:" >> "$ANALYSIS_DIR/token_analysis.txt"
    grep -l "$token" "$ANALYSIS_DIR"/process_* >> "$ANALYSIS_DIR/token_analysis.txt" 2>/dev/null || true
    
    echo "" >> "$ANALYSIS_DIR/token_analysis.txt"
done

# Phase 5: Generate summary report
echo "=== PHASE 5: SUMMARY REPORT GENERATION ===" | tee -a "$ANALYSIS_DIR/analysis.log"

cat > "$ANALYSIS_DIR/manual_analysis_summary.md" << EOF
# Manual Memory Analysis Summary

**Analysis ID**: manual_$(date +%s)
**Timestamp**: $(date)
**Method**: /proc filesystem analysis (no gcore)

## Process Analysis

**Processes Analyzed**: ${#TARGET_PIDS[@]}
**Target PIDs**: ${TARGET_PIDS[@]}

## Token Discovery Results

**Total Unique GitHub Tokens Found**: ${#FOUND_TOKENS[@]}

### Found Tokens Breakdown
EOF

# Count tokens by type
ghp_count=$(echo "${FOUND_TOKENS[@]}" | tr ' ' '\n' | grep -c "^ghp_" || echo "0")
ghs_count=$(echo "${FOUND_TOKENS[@]}" | tr ' ' '\n' | grep -c "^ghs_" || echo "0") 
ghu_count=$(echo "${FOUND_TOKENS[@]}" | tr ' ' '\n' | grep -c "^ghu_" || echo "0")
ghr_count=$(echo "${FOUND_TOKENS[@]}" | tr ' ' '\n' | grep -c "^ghr_" || echo "0")

cat >> "$ANALYSIS_DIR/manual_analysis_summary.md" << EOF

- **Personal Access Tokens (ghp_)**: $ghp_count
- **Session Tokens (ghs_)**: $ghs_count  
- **User Tokens (ghu_)**: $ghu_count
- **App Tokens (ghr_)**: $ghr_count

## Risk Assessment

EOF

if [ "$ghs_count" -gt 0 ]; then
    echo "**Overall Risk**: CRITICAL (GitHub internal session tokens found)" >> "$ANALYSIS_DIR/manual_analysis_summary.md"
elif [ "$ghr_count" -gt 0 ]; then
    echo "**Overall Risk**: HIGH (GitHub app tokens found)" >> "$ANALYSIS_DIR/manual_analysis_summary.md"
elif [ "$ghp_count" -gt 0 ]; then
    echo "**Overall Risk**: MEDIUM (Personal access tokens found)" >> "$ANALYSIS_DIR/manual_analysis_summary.md"
else
    echo "**Overall Risk**: LOW (No critical tokens found)" >> "$ANALYSIS_DIR/manual_analysis_summary.md"
fi

cat >> "$ANALYSIS_DIR/manual_analysis_summary.md" << EOF

## Key Findings

EOF

for token in "${FOUND_TOKENS[@]}"; do
    if [[ "$token" == ghs_* ]]; then
        echo "- **CRITICAL**: GitHub internal session token found (${token:0:10}...)" >> "$ANALYSIS_DIR/manual_analysis_summary.md"
    elif [[ "$token" == ghr_* ]]; then
        echo "- **HIGH**: GitHub app token found (${token:0:10}...)" >> "$ANALYSIS_DIR/manual_analysis_summary.md"
    elif [[ "$token" == ghp_* ]]; then
        echo "- **MEDIUM**: GitHub personal access token found (${token:0:10}...)" >> "$ANALYSIS_DIR/manual_analysis_summary.md"
    fi
done

cat >> "$ANALYSIS_DIR/manual_analysis_summary.md" << EOF

## Analysis Files Generated

- \`baseline_env.txt\` - Environment variables baseline
- \`baseline_processes.txt\` - Process baseline
- \`found_tokens.txt\` - Raw token search results
- \`token_analysis.txt\` - Detailed token analysis
- \`process_*_info.txt\` - Individual process information
- \`process_*_strings.txt\` - Extracted strings per process
- \`process_*_environ.txt\` - Environment variables per process

## Next Steps

EOF

if [ "$ghs_count" -gt 0 ]; then
    cat >> "$ANALYSIS_DIR/manual_analysis_summary.md" << EOF
1. **IMMEDIATE**: Investigate GitHub internal session token exposure
2. **IMMEDIATE**: Validate token permissions and scope
3. Report findings to GitHub security team
4. Implement memory clearing procedures
EOF
else
    cat >> "$ANALYSIS_DIR/manual_analysis_summary.md" << EOF
1. Review found tokens for legitimacy
2. Implement secure token handling practices
3. Monitor for future token exposure
EOF
fi

echo "Manual memory analysis completed!" | tee -a "$ANALYSIS_DIR/analysis.log"
echo "Results saved in: $ANALYSIS_DIR" | tee -a "$ANALYSIS_DIR/analysis.log"
echo "Summary report: $ANALYSIS_DIR/manual_analysis_summary.md" | tee -a "$ANALYSIS_DIR/analysis.log"

# Display summary
echo ""
echo "=== ANALYSIS SUMMARY ==="
cat "$ANALYSIS_DIR/manual_analysis_summary.md"