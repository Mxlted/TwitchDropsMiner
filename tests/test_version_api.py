import src
from src.version import __version__
from src.web.app import _is_newer_version


def test_package_version_matches_single_source_of_truth():
    assert src.__version__ == __version__


def test_version_comparison_uses_numeric_parts():
    assert _is_newer_version("1.2.4", "1.10.0")
    assert _is_newer_version("1.2.4", "v1.2.5")
    assert not _is_newer_version("1.10.0", "1.2.4")
    assert not _is_newer_version("1.2.4", "1.2.4")
