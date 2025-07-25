#!/usr/bin/env python3
"""
Precise Token Analysis for Found GitHub Tokens
Analyzes the tokens discovered in the manual memory analysis
"""

import re
import json
import hashlib
import requests
from datetime import datetime

def analyze_found_tokens():
    """Analyze the specific tokens found in the memory analysis"""
    
    # Tokens found in the analysis
    found_tokens = {
        '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': {
            'source': 'GIT_TOKEN environment variable',
            'process_ids': ['3334'],
            'type': 'ghp_',
            'classification': 'Personal Access Token'
        },
        'ghs_UEK6jNF6Cor13V6XIEpfWToF1Omvvi3EcDAZ': {
            'source': 'GITHUB_TOKEN environment variable', 
            'process_ids': ['3334'],
            'type': 'ghs_',
            'classification': 'Session Token'
        },
        'ghu_OyifOMLztZQb1D6zZB8ubRCzCeRlLy0xnZ9q': {
            'source': 'GITHUB_PERSONAL_ACCESS_TOKEN and GITHUB_COPILOT_API_TOKEN environment variables',
            'process_ids': ['2104', '3334'],
            'type': 'ghu_',
            'classification': 'User Token'
        }
    }
    
    analysis_results = {
        'analysis_metadata': {
            'timestamp': datetime.now().isoformat(),
            'analysis_id': 'precise_token_analysis',
            'method': 'manual_token_extraction'
        },
        'summary': {
            'total_unique_tokens': len(found_tokens),
            'token_types': {
                'ghp_': 0,
                'ghs_': 0, 
                'ghu_': 0,
                'ghr_': 0
            },
            'risk_levels': {
                'CRITICAL': 0,
                'HIGH': 0,
                'MEDIUM': 0,
                'LOW': 0
            }
        },
        'detailed_analysis': []
    }
    
    print("=== PRECISE TOKEN ANALYSIS ===")
    print(f"Analyzing {len(found_tokens)} unique GitHub tokens found in memory")
    print()
    
    for token, metadata in found_tokens.items():
        print(f"Analyzing token: {token[:10]}...")
        
        # Determine ownership and risk
        ownership = determine_token_ownership(token, metadata)
        risk_level = assess_token_risk(token, metadata, ownership)
        
        # Validate token if possible
        validation_result = validate_github_token_safe(token)
        
        # Update summary counters
        token_type = metadata['type']
        analysis_results['summary']['token_types'][token_type] = analysis_results['summary']['token_types'].get(token_type, 0) + 1
        analysis_results['summary']['risk_levels'][risk_level] = analysis_results['summary']['risk_levels'].get(risk_level, 0) + 1
        
        token_analysis = {
            'token_hash': hashlib.sha256(token.encode()).hexdigest()[:16],
            'token_prefix': metadata['type'],
            'token_classification': metadata['classification'],
            'source': metadata['source'],
            'process_ids': metadata['process_ids'],
            'ownership': ownership,
            'risk_level': risk_level,
            'validation_result': validation_result,
            'security_implications': analyze_security_implications(token, metadata, ownership, validation_result)
        }
        
        analysis_results['detailed_analysis'].append(token_analysis)
        
        # Print analysis for this token
        print(f"  Token Type: {metadata['classification']} ({metadata['type']})")
        print(f"  Source: {metadata['source']}")
        print(f"  Ownership: {ownership}")
        print(f"  Risk Level: {risk_level}")
        print(f"  Validation: {'Valid' if validation_result['valid'] else 'Invalid/Error'}")
        if validation_result.get('user_info'):
            print(f"  Associated User: {validation_result['user_info'].get('login', 'Unknown')}")
        print()
    
    # Generate summary
    print("=== SUMMARY ===")
    print(f"Total Tokens Found: {analysis_results['summary']['total_unique_tokens']}")
    print("Token Type Breakdown:")
    for token_type, count in analysis_results['summary']['token_types'].items():
        if count > 0:
            print(f"  {token_type}: {count}")
    
    print("\nRisk Level Breakdown:")
    for risk_level, count in analysis_results['summary']['risk_levels'].items():
        if count > 0:
            print(f"  {risk_level}: {count}")
    
    # Determine overall risk
    if analysis_results['summary']['risk_levels']['CRITICAL'] > 0:
        overall_risk = 'CRITICAL'
    elif analysis_results['summary']['risk_levels']['HIGH'] > 0:
        overall_risk = 'HIGH'
    elif analysis_results['summary']['risk_levels']['MEDIUM'] > 0:
        overall_risk = 'MEDIUM'
    else:
        overall_risk = 'LOW'
    
    print(f"\nOverall Risk Assessment: {overall_risk}")
    
    # Key findings
    print("\n=== KEY FINDINGS ===")
    
    critical_findings = []
    high_findings = []
    
    for analysis in analysis_results['detailed_analysis']:
        if analysis['risk_level'] == 'CRITICAL':
            critical_findings.append(f"CRITICAL: {analysis['token_classification']} with {analysis['ownership']} ownership found in {analysis['source']}")
        elif analysis['risk_level'] == 'HIGH':
            high_findings.append(f"HIGH: {analysis['token_classification']} with {analysis['ownership']} ownership found in {analysis['source']}")
    
    for finding in critical_findings:
        print(f"🔴 {finding}")
    
    for finding in high_findings:
        print(f"🟠 {finding}")
    
    # Special analysis for GitHub internal tokens
    github_internal_tokens = [a for a in analysis_results['detailed_analysis'] if a['ownership'] == 'GITHUB_INTERNAL']
    if github_internal_tokens:
        print(f"\n⚠️  SECURITY ALERT: {len(github_internal_tokens)} GitHub internal token(s) found in agent memory!")
        for token_analysis in github_internal_tokens:
            print(f"   - {token_analysis['token_classification']} in {token_analysis['source']}")
    
    # Save results
    with open('precise_token_analysis_results.json', 'w') as f:
        json.dump(analysis_results, f, indent=2)
    
    # Create markdown report
    create_markdown_report(analysis_results)
    
    return analysis_results

def determine_token_ownership(token, metadata):
    """Determine if token belongs to user or GitHub internal"""
    if token.startswith('ghs_'):
        return 'GITHUB_INTERNAL'
    elif token.startswith('ghp_') and 'GIT_TOKEN' in metadata['source']:
        return 'USER_PROVIDED'
    elif token.startswith('ghu_'):
        if 'GITHUB_COPILOT_API_TOKEN' in metadata['source']:
            return 'GITHUB_INTERNAL'
        else:
            return 'USER_PROVIDED'
    elif token.startswith(('ghr_', 'ghi_')):
        return 'GITHUB_APP'
    else:
        return 'UNKNOWN'

def assess_token_risk(token, metadata, ownership):
    """Assess risk level based on token type and ownership"""
    risk_matrix = {
        ('GITHUB_INTERNAL', 'ghs_'): 'CRITICAL',
        ('GITHUB_INTERNAL', 'ghu_'): 'HIGH',
        ('GITHUB_APP', 'ghr_'): 'HIGH',
        ('GITHUB_APP', 'ghi_'): 'HIGH',
        ('USER_PROVIDED', 'ghp_'): 'MEDIUM',
        ('USER_PROVIDED', 'ghu_'): 'MEDIUM',
        ('UNKNOWN', 'ghp_'): 'HIGH',
    }
    
    key = (ownership, metadata['type'])
    return risk_matrix.get(key, 'LOW')

def validate_github_token_safe(token):
    """Safely validate GitHub token without making unauthorized requests"""
    validation_result = {
        'valid': False,
        'user_info': None,
        'rate_limit': None,
        'error': None
    }
    
    try:
        headers = {'Authorization': f'token {token}'}
        
        # Test with minimal read-only endpoint
        response = requests.get('https://api.github.com/user', headers=headers, timeout=10)
        
        if response.status_code == 200:
            validation_result['valid'] = True
            user_data = response.json()
            validation_result['user_info'] = {
                'login': user_data.get('login'),
                'id': user_data.get('id'),
                'type': user_data.get('type'),
                'name': user_data.get('name')
            }
            
            # Get rate limit to understand token type
            rate_response = requests.get('https://api.github.com/rate_limit', headers=headers, timeout=10)
            if rate_response.status_code == 200:
                validation_result['rate_limit'] = rate_response.json()
        elif response.status_code == 401:
            validation_result['valid'] = False
            validation_result['error'] = 'Unauthorized - Invalid token'
        else:
            validation_result['error'] = f'HTTP {response.status_code}: {response.text[:100]}'
            
    except Exception as e:
        validation_result['error'] = f'Validation error: {str(e)}'
    
    return validation_result

def analyze_security_implications(token, metadata, ownership, validation_result):
    """Analyze security implications of the found token"""
    implications = []
    
    if ownership == 'GITHUB_INTERNAL':
        implications.append('GitHub internal token exposure - potential for unauthorized GitHub infrastructure access')
        implications.append('May provide access to GitHub internal APIs and services')
        
    if ownership == 'USER_PROVIDED':
        implications.append('User-provided token in memory - standard expected behavior')
        
    if validation_result['valid'] and validation_result.get('user_info'):
        user_type = validation_result['user_info'].get('type', 'Unknown')
        if user_type == 'User':
            implications.append('Token belongs to individual user account')
        elif user_type == 'Organization':
            implications.append('Token belongs to organization account - higher privilege risk')
            
    if len(metadata['process_ids']) > 1:
        implications.append('Token present in multiple processes - broader exposure surface')
        
    return implications

def create_markdown_report(analysis_results):
    """Create a comprehensive markdown report"""
    with open('comprehensive_token_analysis_report.md', 'w') as f:
        f.write("# Comprehensive GitHub Token Analysis Report\n\n")
        f.write(f"**Analysis ID**: {analysis_results['analysis_metadata']['analysis_id']}\n")
        f.write(f"**Timestamp**: {analysis_results['analysis_metadata']['timestamp']}\n")
        f.write(f"**Method**: {analysis_results['analysis_metadata']['method']}\n\n")
        
        # Summary
        f.write("## Executive Summary\n\n")
        f.write(f"**Total Tokens Analyzed**: {analysis_results['summary']['total_unique_tokens']}\n\n")
        
        # Risk assessment
        critical_count = analysis_results['summary']['risk_levels']['CRITICAL']
        high_count = analysis_results['summary']['risk_levels']['HIGH']
        
        if critical_count > 0:
            f.write("**🔴 CRITICAL SECURITY ISSUE IDENTIFIED**\n\n")
            f.write(f"Found {critical_count} CRITICAL risk token(s) that require immediate attention.\n\n")
        elif high_count > 0:
            f.write("**🟠 HIGH SECURITY RISK IDENTIFIED**\n\n")
            f.write(f"Found {high_count} HIGH risk token(s) that require prompt investigation.\n\n")
        
        # Token breakdown
        f.write("## Token Type Analysis\n\n")
        f.write("| Token Type | Count | Description |\n")
        f.write("|------------|-------|-------------|\n")
        for token_type, count in analysis_results['summary']['token_types'].items():
            if count > 0:
                type_desc = {
                    'ghp_': 'Personal Access Token',
                    'ghs_': 'Session Token (typically internal)',
                    'ghu_': 'User Token',
                    'ghr_': 'App Token'
                }.get(token_type, 'Unknown')
                f.write(f"| {token_type} | {count} | {type_desc} |\n")
        f.write("\n")
        
        # Detailed findings
        f.write("## Detailed Findings\n\n")
        for i, analysis in enumerate(analysis_results['detailed_analysis'], 1):
            f.write(f"### Finding #{i}: {analysis['token_classification']}\n\n")
            f.write(f"- **Token Type**: {analysis['token_prefix']}\n")
            f.write(f"- **Source**: {analysis['source']}\n")
            f.write(f"- **Ownership**: {analysis['ownership']}\n")
            f.write(f"- **Risk Level**: {analysis['risk_level']}\n")
            f.write(f"- **Process IDs**: {', '.join(analysis['process_ids'])}\n")
            
            if analysis['validation_result']['valid']:
                user_info = analysis['validation_result']['user_info']
                f.write(f"- **Validation**: ✅ Valid\n")
                f.write(f"- **Associated User**: {user_info.get('login', 'Unknown')}\n")
                f.write(f"- **User Type**: {user_info.get('type', 'Unknown')}\n")
            else:
                error = analysis['validation_result'].get('error', 'Unknown')
                f.write(f"- **Validation**: ❌ Invalid ({error})\n")
            
            f.write("\n**Security Implications**:\n")
            for implication in analysis['security_implications']:
                f.write(f"- {implication}\n")
            f.write("\n")
        
        # Recommendations
        f.write("## Recommendations\n\n")
        
        github_internal_count = sum(1 for a in analysis_results['detailed_analysis'] if a['ownership'] == 'GITHUB_INTERNAL')
        
        if github_internal_count > 0:
            f.write("### Immediate Actions Required\n\n")
            f.write("1. **URGENT**: Report GitHub internal token exposure to GitHub Security\n")
            f.write("2. **URGENT**: Rotate all GitHub internal session tokens\n")
            f.write("3. **URGENT**: Audit GitHub Copilot agent token handling procedures\n")
            f.write("4. Implement secure memory clearing after token usage\n")
            f.write("5. Review GitHub Copilot infrastructure security practices\n\n")
        
        f.write("### General Recommendations\n\n")
        f.write("1. Implement token lifecycle management in agent processes\n")
        f.write("2. Add memory sanitization procedures\n")
        f.write("3. Monitor for unauthorized token usage\n")
        f.write("4. Regular security audits of agent memory contents\n")
        f.write("5. Implement least-privilege token scoping\n")

if __name__ == "__main__":
    analyze_found_tokens()