#!/usr/bin/env python3
"""
Enhanced Secret Analyzer for GitHub Copilot Agent Memory Analysis
Implements comprehensive secret detection, validation, and risk assessment
"""

import re
import sys
import json
import os
import subprocess
import hashlib
import time
from datetime import datetime
from typing import Dict, List, Any, Optional
import requests

class EnhancedSecretAnalyzer:
    def __init__(self):
        self.patterns = {
            'github_pat': r'ghp_[A-Za-z0-9]{36}',
            'github_session': r'ghs_[A-Za-z0-9]{36}',
            'github_user': r'ghu_[A-Za-z0-9]{36}',
            'github_app': r'ghr_[A-Za-z0-9]{36}',
            'github_install': r'ghi_[A-Za-z0-9]{36}',
            'github_oauth': r'gho_[A-Za-z0-9]{36}',
            'jwt': r'eyJ[A-Za-z0-9_-]*\.[A-Za-z0-9_-]*\.[A-Za-z0-9_-]*',
            'bearer': r'Bearer [A-Za-z0-9_-]+',
            'api_key': r'[A-Za-z0-9]{32,64}',
            'aws_key': r'AKIA[0-9A-Z]{16}',
            'azure_key': r'[A-Za-z0-9+/]{32,}={0,2}',
        }
        
        self.known_user_tokens = []
        self.baseline_data = {}
        self.analysis_results = []
        
    def document_baseline(self, output_dir: str) -> Dict[str, Any]:
        """Document baseline environment and process state"""
        baseline = {
            'timestamp': datetime.now().isoformat(),
            'environment_tokens': [],
            'github_processes': [],
            'network_connections': [],
            'memory_usage': {}
        }
        
        # Document environment variables with secrets
        try:
            env_output = subprocess.check_output(['env'], text=True)
            for line in env_output.split('\n'):
                if any(keyword in line.lower() for keyword in ['token', 'key', 'secret', 'auth', 'pass']):
                    baseline['environment_tokens'].append(line)
                    if 'GIT_TOKEN' in line:
                        self.known_user_tokens.append(line.split('=')[1] if '=' in line else '')
        except Exception as e:
            print(f"Error documenting environment: {e}")
        
        # Document GitHub-related processes
        try:
            ps_output = subprocess.check_output(['ps', 'auxf'], text=True)
            for line in ps_output.split('\n'):
                if any(keyword in line.lower() for keyword in ['github', 'copilot', 'node', 'runner']):
                    baseline['github_processes'].append(line.strip())
        except Exception as e:
            print(f"Error documenting processes: {e}")
        
        # Save baseline to file
        with open(f"{output_dir}/baseline.json", 'w') as f:
            json.dump(baseline, f, indent=2)
        
        self.baseline_data = baseline
        return baseline
    
    def collect_memory_dumps(self, output_dir: str) -> List[str]:
        """Collect memory dumps from target processes"""
        dump_files = []
        
        try:
            # Find GitHub-related processes
            ps_output = subprocess.check_output(['ps', '-eo', 'pid,comm'], text=True)
            target_pids = []
            
            for line in ps_output.split('\n')[1:]:  # Skip header
                if line.strip():
                    parts = line.strip().split()
                    if len(parts) >= 2:
                        pid, comm = parts[0], parts[1]
                        if any(keyword in comm.lower() for keyword in ['node', 'runner', 'github']):
                            target_pids.append(pid)
            
            # Create memory dumps
            for pid in target_pids:
                try:
                    dump_file = f"{output_dir}/memory_dump_{pid}.core"
                    print(f"Creating memory dump for PID {pid}...")
                    subprocess.run(['gcore', '-o', dump_file.replace('.core', ''), pid], 
                                 check=True, capture_output=True)
                    if os.path.exists(dump_file):
                        dump_files.append(dump_file)
                except subprocess.CalledProcessError as e:
                    print(f"Failed to dump PID {pid}: {e}")
                    
        except Exception as e:
            print(f"Error collecting memory dumps: {e}")
        
        return dump_files
    
    def analyze_memory_dump(self, dump_file: str, output_dir: str) -> Dict[str, Any]:
        """Analyze a single memory dump for secrets"""
        results = {
            'file': dump_file,
            'timestamp': datetime.now().isoformat(),
            'secrets': [],
            'analysis': {},
            'memory_regions': [],
            'validation_results': {}
        }
        
        print(f"Analyzing memory dump: {dump_file}")
        
        try:
            # Extract strings from memory dump
            strings_output = subprocess.check_output(['strings', dump_file], text=True)
            
            # Search for secrets using patterns
            for secret_type, pattern in self.patterns.items():
                matches = re.finditer(pattern, strings_output, re.MULTILINE)
                for match in matches:
                    secret_value = match.group()
                    secret_info = {
                        'type': secret_type,
                        'value_hash': hashlib.sha256(secret_value.encode()).hexdigest()[:16],
                        'position': match.start(),
                        'context': self.extract_context(strings_output, match.start()),
                        'ownership': self.determine_ownership(secret_value),
                        'risk_level': self.assess_risk(secret_type, secret_value),
                        'memory_region': self.identify_memory_region(match.start()),
                        'validation_result': None
                    }
                    
                    # Validate GitHub tokens
                    if secret_type.startswith('github'):
                        secret_info['validation_result'] = self.validate_github_token(secret_value)
                    
                    results['secrets'].append(secret_info)
                    
        except Exception as e:
            print(f"Error analyzing {dump_file}: {e}")
            results['error'] = str(e)
        
        # Save individual results
        output_file = f"{output_dir}/analysis_{os.path.basename(dump_file)}.json"
        with open(output_file, 'w') as f:
            json.dump(results, f, indent=2)
        
        return results
    
    def extract_context(self, content: str, position: int, window: int = 200) -> str:
        """Extract context around a found secret"""
        start = max(0, position - window)
        end = min(len(content), position + window)
        context = content[start:end]
        # Clean up the context to remove binary data
        context = re.sub(r'[^\x20-\x7E\n]', '.', context)
        return context
    
    def determine_ownership(self, token: str) -> str:
        """Determine if token belongs to user or GitHub internal"""
        if token in self.known_user_tokens:
            return 'USER_KNOWN'
        elif token.startswith('ghs_'):
            return 'GITHUB_INTERNAL'
        elif token.startswith('ghp_'):
            return 'USER_UNKNOWN'
        elif token.startswith(('ghr_', 'ghi_')):
            return 'GITHUB_APP'
        else:
            return 'UNKNOWN'
    
    def assess_risk(self, token_type: str, token_value: str) -> str:
        """Assess risk level of a found token"""
        ownership = self.determine_ownership(token_value)
        
        risk_matrix = {
            ('GITHUB_INTERNAL', 'github_session'): 'CRITICAL',
            ('GITHUB_APP', 'github_app'): 'HIGH',
            ('USER_UNKNOWN', 'github_pat'): 'HIGH',
            ('USER_KNOWN', 'github_pat'): 'MEDIUM',
            ('UNKNOWN', 'jwt'): 'MEDIUM',
            ('UNKNOWN', 'bearer'): 'HIGH'
        }
        
        return risk_matrix.get((ownership, token_type), 'LOW')
    
    def identify_memory_region(self, position: int) -> str:
        """Identify memory region type (simplified)"""
        if position < 0x1000000:
            return "STACK"
        elif position < 0x10000000:
            return "HEAP"
        else:
            return "MAPPED"
    
    def validate_github_token(self, token: str) -> Dict[str, Any]:
        """Safely validate GitHub token"""
        validation_result = {
            'valid': False,
            'user_info': None,
            'permissions': [],
            'rate_limit': None,
            'error': None
        }
        
        try:
            # Test with GitHub API user endpoint (read-only)
            headers = {'Authorization': f'token {token}'}
            response = requests.get('https://api.github.com/user', headers=headers, timeout=10)
            
            if response.status_code == 200:
                validation_result['valid'] = True
                validation_result['user_info'] = response.json()
                
                # Get rate limit info to understand token type
                rate_response = requests.get('https://api.github.com/rate_limit', headers=headers, timeout=10)
                if rate_response.status_code == 200:
                    validation_result['rate_limit'] = rate_response.json()
                    
            elif response.status_code == 401:
                validation_result['valid'] = False
                validation_result['error'] = 'Invalid token'
            else:
                validation_result['error'] = f'HTTP {response.status_code}'
                
        except Exception as e:
            validation_result['error'] = str(e)
        
        return validation_result
    
    def generate_comprehensive_report(self, output_dir: str, all_results: List[Dict]) -> str:
        """Generate comprehensive analysis report"""
        report = {
            'analysis_metadata': {
                'timestamp': datetime.now().isoformat(),
                'analysis_id': f"enhanced_{int(time.time())}",
                'analyst': 'enhanced_secret_analyzer',
                'methodology_version': '2.0'
            },
            'system_baseline': self.baseline_data,
            'summary': {
                'total_dumps_analyzed': len(all_results),
                'total_secrets_found': sum(len(r.get('secrets', [])) for r in all_results),
                'critical_findings': 0,
                'high_risk_findings': 0,
                'github_internal_tokens': 0,
                'user_tokens': 0
            },
            'detailed_findings': [],
            'risk_assessment': {},
            'recommendations': []
        }
        
        # Aggregate findings
        all_secrets = []
        for result in all_results:
            for secret in result.get('secrets', []):
                all_secrets.append(secret)
                
                # Update summary counters
                if secret['risk_level'] == 'CRITICAL':
                    report['summary']['critical_findings'] += 1
                elif secret['risk_level'] == 'HIGH':
                    report['summary']['high_risk_findings'] += 1
                    
                if secret['ownership'] == 'GITHUB_INTERNAL':
                    report['summary']['github_internal_tokens'] += 1
                elif secret['ownership'].startswith('USER'):
                    report['summary']['user_tokens'] += 1
        
        # Deduplicate secrets (same token found in multiple dumps)
        unique_secrets = {}
        for secret in all_secrets:
            key = secret['value_hash']
            if key not in unique_secrets:
                unique_secrets[key] = secret
        
        report['detailed_findings'] = list(unique_secrets.values())
        
        # Risk assessment
        report['risk_assessment'] = {
            'overall_risk': 'CRITICAL' if report['summary']['critical_findings'] > 0 else 'HIGH' if report['summary']['high_risk_findings'] > 0 else 'MEDIUM',
            'github_internal_exposure': report['summary']['github_internal_tokens'] > 0,
            'cross_tenant_risk': 'LOW',  # Based on previous analysis
            'data_exfiltration_risk': 'HIGH' if report['summary']['github_internal_tokens'] > 0 else 'MEDIUM'
        }
        
        # Recommendations
        if report['summary']['github_internal_tokens'] > 0:
            report['recommendations'].extend([
                'IMMEDIATE: Rotate all GitHub internal session tokens',
                'Implement secure memory handling for GitHub tokens',
                'Add memory clearing after token usage',
                'Conduct security audit of token lifecycle'
            ])
        
        if report['summary']['user_tokens'] > 0:
            report['recommendations'].extend([
                'Review user token permissions and scope',
                'Implement token rotation policies',
                'Monitor token usage patterns'
            ])
        
        # Save comprehensive report
        report_file = f"{output_dir}/comprehensive_analysis_report.json"
        with open(report_file, 'w') as f:
            json.dump(report, f, indent=2)
        
        # Create markdown summary
        markdown_file = f"{output_dir}/analysis_summary.md"
        self.create_markdown_summary(report, markdown_file)
        
        return report_file
    
    def create_markdown_summary(self, report: Dict, output_file: str):
        """Create a markdown summary of the analysis"""
        with open(output_file, 'w') as f:
            f.write("# Enhanced Secret Analysis Report\n\n")
            f.write(f"**Analysis ID**: {report['analysis_metadata']['analysis_id']}\n")
            f.write(f"**Timestamp**: {report['analysis_metadata']['timestamp']}\n")
            f.write(f"**Overall Risk Level**: {report['risk_assessment']['overall_risk']}\n\n")
            
            f.write("## Summary\n\n")
            f.write(f"- **Total Memory Dumps Analyzed**: {report['summary']['total_dumps_analyzed']}\n")
            f.write(f"- **Total Secrets Found**: {report['summary']['total_secrets_found']}\n")
            f.write(f"- **Critical Findings**: {report['summary']['critical_findings']}\n")
            f.write(f"- **High Risk Findings**: {report['summary']['high_risk_findings']}\n")
            f.write(f"- **GitHub Internal Tokens**: {report['summary']['github_internal_tokens']}\n")
            f.write(f"- **User Tokens**: {report['summary']['user_tokens']}\n\n")
            
            f.write("## Key Findings\n\n")
            for finding in report['detailed_findings']:
                f.write(f"### {finding['type'].title()} Token\n")
                f.write(f"- **Risk Level**: {finding['risk_level']}\n")
                f.write(f"- **Ownership**: {finding['ownership']}\n")
                f.write(f"- **Memory Region**: {finding['memory_region']}\n")
                if finding.get('validation_result'):
                    val_result = finding['validation_result']
                    f.write(f"- **Valid**: {val_result['valid']}\n")
                    if val_result.get('user_info'):
                        f.write(f"- **Associated User**: {val_result['user_info'].get('login', 'Unknown')}\n")
                f.write("\n")
            
            f.write("## Recommendations\n\n")
            for recommendation in report['recommendations']:
                f.write(f"- {recommendation}\n")
    
    def run_comprehensive_analysis(self) -> str:
        """Run the complete enhanced analysis"""
        analysis_id = f"enhanced_{int(time.time())}"
        output_dir = f"analysis_{analysis_id}"
        os.makedirs(output_dir, exist_ok=True)
        
        print(f"Starting enhanced secret analysis - ID: {analysis_id}")
        print(f"Output directory: {output_dir}")
        
        # Phase 0: Document baseline
        print("Phase 0: Documenting baseline...")
        self.document_baseline(output_dir)
        
        # Phase 1: Collect memory dumps
        print("Phase 1: Collecting memory dumps...")
        dump_files = self.collect_memory_dumps(output_dir)
        print(f"Collected {len(dump_files)} memory dumps")
        
        # Phase 2: Analyze dumps
        print("Phase 2: Analyzing memory dumps...")
        all_results = []
        for dump_file in dump_files:
            result = self.analyze_memory_dump(dump_file, output_dir)
            all_results.append(result)
            self.analysis_results.append(result)
        
        # Phase 3: Generate comprehensive report
        print("Phase 3: Generating comprehensive report...")
        report_file = self.generate_comprehensive_report(output_dir, all_results)
        
        print(f"Analysis complete. Report saved to: {report_file}")
        return output_dir

def main():
    analyzer = EnhancedSecretAnalyzer()
    output_dir = analyzer.run_comprehensive_analysis()
    print(f"\nEnhanced secret analysis completed. Results in: {output_dir}")
    
    # Print summary
    summary_file = f"{output_dir}/analysis_summary.md"
    if os.path.exists(summary_file):
        print("\n" + "="*50)
        print("ANALYSIS SUMMARY")
        print("="*50)
        with open(summary_file, 'r') as f:
            print(f.read())

if __name__ == "__main__":
    main()